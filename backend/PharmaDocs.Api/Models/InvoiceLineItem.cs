namespace PharmaDocs.Api.Models;

/// <summary>
/// Eén lijn van een factuur (bv. een besteld product).
/// Meerdere lijnitems horen bij één <see cref="ExtractedInvoice"/>.
/// </summary>
public class InvoiceLineItem
{
    public Guid Id { get; set; }

    /// <summary>FK naar de factuur waartoe deze lijn behoort.</summary>
    public Guid ExtractedInvoiceId { get; set; }

    /// <summary>Omschrijving van het lijnitem (product/dienst).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Aantal eenheden.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Eenheidsprijs.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Totaal voor deze lijn (Quantity × UnitPrice).</summary>
    public decimal LineTotal { get; set; }

    // --- Navigatie ---

    public ExtractedInvoice ExtractedInvoice { get; set; } = null!;
}
