using System.ComponentModel.DataAnnotations;

namespace PharmaDocs.Api.DTOs.Documents;

/// <summary>
/// Handmatige correctie van een geëxtraheerde factuur. De lijst met lijnitems
/// vervangt de bestaande volledig (toevoegen/verwijderen is dus mogelijk).
/// </summary>
public record UpdateInvoiceRequest(
    [Required, MaxLength(256)] string SupplierName,
    [Required, MaxLength(128)] string InvoiceNumber,
    DateOnly? InvoiceDate,
    [Range(0, 9_999_999)] decimal SubtotalAmount,
    [Range(0, 100)] decimal? VatRate,
    [Range(0, 9_999_999)] decimal VatAmount,
    [Range(0, 9_999_999)] decimal TotalAmount,
    [Required, MaxLength(8)] string Currency,
    List<UpdateLineItemRequest> LineItems);

/// <summary>Eén te bewaren factuurlijn.</summary>
public record UpdateLineItemRequest(
    [Required, MaxLength(512)] string Description,
    [Range(0, 9_999_999)] decimal Quantity,
    [Range(0, 9_999_999)] decimal UnitPrice,
    [Range(0, 9_999_999)] decimal LineTotal);
