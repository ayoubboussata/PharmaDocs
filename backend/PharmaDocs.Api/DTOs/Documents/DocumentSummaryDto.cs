namespace PharmaDocs.Api.DTOs.Documents;

/// <summary>
/// Compacte weergave van een document voor de overzichtstabel.
/// De belangrijkste geëxtraheerde velden zijn inline meegegeven (of null zolang niet verwerkt).
/// </summary>
public record DocumentSummaryDto(
    Guid Id,
    string FileName,
    string Status,
    DateTime UploadedAt,
    string? SupplierName,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    decimal? TotalAmount,
    string? Currency,
    string? Category);
