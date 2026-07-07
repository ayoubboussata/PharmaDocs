using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.Common.Mapping;
using PharmaDocs.Api.DTOs.Documents;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;
using PharmaDocs.Api.Repositories;

namespace PharmaDocs.Api.Services;

public class DocumentService : IDocumentService
{
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB, gelijk aan de AI-service

    private readonly IDocumentRepository _repository;
    private readonly IInvoiceExtractionClient _extractionClient;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository repository,
        IInvoiceExtractionClient extractionClient,
        ILogger<DocumentService> logger)
    {
        _repository = repository;
        _extractionClient = extractionClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var documents = await _repository.GetAllAsync(userId, ct);
        return documents.Select(d => d.ToSummaryDto()).ToList();
    }

    public async Task<DocumentDetailDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var document = await _repository.GetByIdAsync(id, userId, ct);
        return document?.ToDetailDto();
    }

    public async Task<DocumentDetailDto> UploadAndExtractAsync(IFormFile file, Guid userId, CancellationToken ct = default)
    {
        ValidateUpload(file);

        // 1. Document eerst als Pending vastleggen: de upload is nu veilig bewaard,
        //    ongeacht wat er met de AI-extractie gebeurt.
        var document = new Document
        {
            Id = Guid.NewGuid(),
            UserId = userId, // eigenaar = uploader (privé per gebruiker)
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            Status = DocumentStatus.Pending,
        };
        await _repository.AddAsync(document, ct);

        // 2. De AI-service aanroepen en het resultaat op het document vastleggen.
        try
        {
            await using var stream = file.OpenReadStream();
            var extraction = await _extractionClient.ExtractAsync(stream, file.FileName, file.ContentType, ct);

            // Enkel echte facturen/bestelbonnen: is het iets anders (attest, brief, …),
            // dan weigeren we de upload en bewaren we géén bogus record.
            if (!extraction.IsInvoice)
            {
                _logger.LogInformation("Upload {DocumentId} geweigerd: geen factuur.", document.Id);
                await _repository.DeleteAsync(document, ct);
                throw new UnprocessableEntityException(
                    "Dit document lijkt geen factuur of bestelbon te zijn. Enkel facturen worden verwerkt.");
            }

            document.ExtractedInvoice = BuildInvoice(document.Id, extraction);
            document.Status = DocumentStatus.Processed;
            document.ErrorMessage = null;
        }
        catch (AiExtractionException ex)
        {
            // Geen crash: de fout hoort bij dít document → Failed + boodschap.
            _logger.LogWarning(ex, "Extractie mislukt voor document {DocumentId}", document.Id);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
        }

        await _repository.SaveChangesAsync(ct);
        return document.ToDetailDto();
    }

    public async Task<DocumentDetailDto?> UpdateInvoiceAsync(
        Guid id, Guid userId, UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        var document = await _repository.GetTrackedByIdAsync(id, userId, ct);
        if (document is null)
            return null;
        if (document.ExtractedInvoice is null)
            throw new BadRequestException("Dit document heeft geen extractie om te corrigeren.");

        var invoice = document.ExtractedInvoice;
        invoice.SupplierName = request.SupplierName;
        invoice.InvoiceNumber = request.InvoiceNumber;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.SubtotalAmount = request.SubtotalAmount;
        invoice.VatRate = request.VatRate;
        invoice.VatAmount = request.VatAmount;
        invoice.TotalAmount = request.TotalAmount;
        invoice.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency;
        invoice.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category;

        // Lijnitems volledig vervangen: eenvoudig en robuust bij toevoegen/verwijderen.
        // De oude (nu wees geworden) lijnen worden door EF verwijderd; de nieuwe
        // krijgen een gegenereerde sleutel → INSERT (geen Id zelf zetten).
        invoice.LineItems.Clear();
        foreach (var line in request.LineItems)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = line.LineTotal,
            });
        }

        await _repository.SaveChangesAsync(ct);
        return document.ToDetailDto();
    }

    // Belgische notatie: komma als decimaalteken, past bij het puntkomma-scheidingsteken.
    private static readonly CultureInfo NlBe = CultureInfo.GetCultureInfo("nl-BE");

    public async Task<byte[]> ExportCsvAsync(Guid userId, CancellationToken ct = default)
    {
        var documents = await _repository.GetAllAsync(userId, ct);

        var sb = new StringBuilder();
        // Puntkomma als scheidingsteken → opent netjes in Excel met Belgische regio-instellingen.
        sb.AppendLine(string.Join(';', new[]
        {
            "Bestand", "Leverancier", "Factuurnummer", "Factuurdatum", "Categorie",
            "Subtotaal", "Btw-tarief", "Btw-bedrag", "Totaal", "Munt", "Status", "Geuploaded",
        }));

        foreach (var doc in documents)
        {
            var inv = doc.ExtractedInvoice;
            sb.AppendLine(string.Join(';', new[]
            {
                Csv(doc.FileName),
                Csv(inv?.SupplierName),
                Csv(inv?.InvoiceNumber),
                Csv(inv?.InvoiceDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(inv?.Category),
                Money(inv?.SubtotalAmount),
                inv?.VatRate is { } rate ? Csv(rate.ToString("0.##", NlBe)) : string.Empty,
                Money(inv?.VatAmount),
                Money(inv?.TotalAmount),
                Csv(inv?.Currency),
                Csv(doc.Status.ToString()),
                Csv(doc.UploadedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
            }));
        }

        // UTF-8 met BOM zodat Excel accenten (é, ë) correct toont.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Money(decimal? value) =>
        value is { } v ? v.ToString("0.00", NlBe) : string.Empty;

    /// <summary>RFC 4180-escaping: velden met ; " of een regeleinde tussen quotes.</summary>
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static void ValidateUpload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("Geen bestand ontvangen.");
        if (file.Length > MaxBytes)
            throw new PayloadTooLargeException("Bestand te groot (max. 10 MB).");

        var contentType = file.ContentType;
        var isPdf = contentType is "application/pdf" or "application/octet-stream"
            || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new UnsupportedMediaTypeException("Enkel PDF-bestanden worden ondersteund.");
    }

    // Sleutels (Id) bewust niet zelf zetten: het zijn nieuwe entiteiten die via de
    // navigatie aan het reeds-getrackte document hangen. EF genereert de Guid's en
    // markeert ze zo als toe-te-voegen (INSERT i.p.v. UPDATE).
    private static ExtractedInvoice BuildInvoice(Guid documentId, InvoiceExtractionResult extraction) => new()
    {
        DocumentId = documentId,
        SupplierName = extraction.SupplierName,
        InvoiceNumber = extraction.InvoiceNumber,
        InvoiceDate = extraction.InvoiceDate,
        SubtotalAmount = extraction.SubtotalAmount,
        VatRate = extraction.VatRate,
        VatAmount = extraction.VatAmount,
        TotalAmount = extraction.TotalAmount,
        Currency = extraction.Currency,
        Category = extraction.Category,
        CreatedAt = DateTime.UtcNow,
        LineItems = extraction.LineItems.Select(l => new InvoiceLineItem
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.LineTotal,
        }).ToList(),
    };
}
