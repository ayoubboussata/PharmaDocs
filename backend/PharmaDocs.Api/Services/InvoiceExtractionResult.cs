namespace PharmaDocs.Api.Services;

/// <summary>
/// Het resultaat van de AI-extractie zoals teruggegeven door de Python-service,
/// al vertaald naar .NET-types (datum als <see cref="DateOnly"/>). Los van de
/// wire-DTO's zodat de rest van de backend niet met de HTTP-vorm hoeft te werken.
/// </summary>
public sealed record InvoiceExtractionResult(
    string SupplierName,
    string InvoiceNumber,
    DateOnly? InvoiceDate,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<InvoiceLineResult> LineItems);

/// <summary>Eén geëxtraheerde factuurlijn.</summary>
public sealed record InvoiceLineResult(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);
