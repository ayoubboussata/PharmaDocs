namespace PharmaDocs.Api.Models;

/// <summary>
/// De door de AI (Claude) uit een <see cref="Document"/> geëxtraheerde factuurgegevens.
/// Eén-op-één met het brondocument.
/// </summary>
public class ExtractedInvoice
{
    public Guid Id { get; set; }

    /// <summary>FK naar het brondocument (uniek → één-op-één).</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Naam van de leverancier.</summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>Factuurnummer zoals op het document.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Factuurdatum (enkel datum, geen tijd).</summary>
    public DateOnly? InvoiceDate { get; set; }

    /// <summary>Totaalbedrag van de factuur.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Munteenheid, standaard EUR.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Tijdstip waarop de extractie werd opgeslagen (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    // --- Navigatie ---

    public Document Document { get; set; } = null!;

    /// <summary>De individuele lijnitems van de factuur.</summary>
    public List<InvoiceLineItem> LineItems { get; set; } = new();
}
