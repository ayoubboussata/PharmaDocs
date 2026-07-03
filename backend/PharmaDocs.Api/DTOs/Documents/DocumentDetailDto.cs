namespace PharmaDocs.Api.DTOs.Documents;

/// <summary>Volledige detailweergave van één document met (indien aanwezig) de extractie.</summary>
public record DocumentDetailDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Status,
    DateTime UploadedAt,
    string? ErrorMessage,
    ExtractedInvoiceDto? ExtractedInvoice);
