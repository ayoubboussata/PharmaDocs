using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.DTOs.Documents;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Bedrijfslogica rond documenten. De controller roept enkel deze service aan.
/// </summary>
public interface IDocumentService
{
    Task<IReadOnlyList<DocumentSummaryDto>> GetAllAsync(CancellationToken ct = default);

    Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Ontvangt een geüploade PDF, laat de AI-service de factuur extraheren en bewaart
    /// het resultaat in de tenant, met <paramref name="uploaderId"/> als uploader. Bij
    /// een mislukte extractie wordt het document als <c>Failed</c> vastgelegd
    /// (upload gaat nooit verloren).
    /// </summary>
    Task<DocumentDetailDto> UploadAndExtractAsync(IFormFile file, Guid uploaderId, CancellationToken ct = default);

    /// <summary>
    /// Handmatige correctie van de geëxtraheerde factuur (gedeeld binnen de apotheek).
    /// Geeft het bijgewerkte document terug, of <c>null</c> als het niet bestaat in de tenant.
    /// </summary>
    Task<DocumentDetailDto?> UpdateInvoiceAsync(Guid id, UpdateInvoiceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Bouwt een CSV-export (UTF-8 met BOM, één rij per document) klaar om in Excel
    /// te openen. Zijn er <paramref name="ids"/> meegegeven, dan enkel die documenten;
    /// anders alle facturen van de tenant.
    /// </summary>
    Task<byte[]> ExportCsvAsync(IReadOnlyCollection<Guid>? ids, CancellationToken ct = default);

    /// <summary>
    /// Verwijdert een document van de tenant. Geeft <c>false</c> als het niet bestaat
    /// in de tenant.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
