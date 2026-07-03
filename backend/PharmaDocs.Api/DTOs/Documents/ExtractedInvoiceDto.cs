namespace PharmaDocs.Api.DTOs.Documents;

/// <summary>De geëxtraheerde factuurgegevens inclusief lijnitems.</summary>
public record ExtractedInvoiceDto(
    Guid Id,
    string SupplierName,
    string InvoiceNumber,
    DateOnly? InvoiceDate,
    decimal SubtotalAmount,
    decimal? VatRate,
    decimal VatAmount,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<InvoiceLineItemDto> LineItems);
