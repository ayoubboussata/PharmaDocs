namespace PharmaDocs.Api.DTOs.Documents;

/// <summary>Eén factuurlijn zoals getoond aan de client.</summary>
public record InvoiceLineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);
