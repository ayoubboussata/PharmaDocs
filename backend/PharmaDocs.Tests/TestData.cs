using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;

namespace PharmaDocs.Tests;

/// <summary>Kleine fabriekjes om test-entiteiten en -bestanden op te bouwen.</summary>
internal static class TestData
{
    public static Document Doc(
        string supplier,
        string invoiceNr,
        decimal subtotal,
        decimal vat,
        decimal total,
        string? category = "Geneesmiddelen",
        DateOnly? date = null,
        DocumentStatus status = DocumentStatus.Processed,
        string fileName = "factuur.pdf")
    {
        var docId = Guid.NewGuid();
        var doc = new Document
        {
            Id = docId,
            FileName = fileName,
            ContentType = "application/pdf",
            FileSizeBytes = 1234,
            UploadedAt = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            Status = status,
        };

        // Enkel een verwerkt document heeft een extractie.
        if (status == DocumentStatus.Processed)
        {
            doc.ExtractedInvoice = new ExtractedInvoice
            {
                Id = Guid.NewGuid(),
                DocumentId = docId,
                SupplierName = supplier,
                InvoiceNumber = invoiceNr,
                InvoiceDate = date,
                SubtotalAmount = subtotal,
                VatRate = 21,
                VatAmount = vat,
                TotalAmount = total,
                Currency = "EUR",
                Category = category,
                CreatedAt = DateTime.UtcNow,
            };
        }

        return doc;
    }

    public static IFormFile FakeFile(
        string fileName = "factuur.pdf",
        string contentType = "application/pdf",
        long? size = null,
        byte[]? content = null)
    {
        var bytes = content ?? new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
        var length = size ?? bytes.Length;
        return new FormFile(new MemoryStream(bytes), 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
