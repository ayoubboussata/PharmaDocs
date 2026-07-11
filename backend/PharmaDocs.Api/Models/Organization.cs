namespace PharmaDocs.Api.Models;

/// <summary>
/// Een klant/tenant: één apotheekbedrijf. Alle tenant-data (gebruikers, facturen,
/// kennisbank) hangt via <c>TenantId</c> aan een Organization. Dit is het fundament
/// voor de multi-tenant isolatie (Fase 1).
/// </summary>
public class Organization
{
    /// <summary>
    /// Vaste id van de default-organisatie waaraan alle bestaande (single-tenant) data
    /// wordt gekoppeld bij de migratie. Fase 3 introduceert echte tenants uit de
    /// JWT-claim; tot dan hangt alles aan deze ene organisatie.
    /// </summary>
    public static readonly Guid DefaultId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Standaard-accentkleur (het huidige blauw) voor tenants zonder eigen keuze.</summary>
    public const string DefaultAccentColor = "#2563eb";

    public Guid Id { get; set; }

    /// <summary>Weergavenaam van het apotheekbedrijf (bv. "Apotheek De Wit").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-vriendelijke, unieke sleutel (bv. "apotheek-de-wit").</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Accentkleur van de apotheek (hex, bv. "#2563eb") — per-tenant branding (MT12).</summary>
    public string AccentColor { get; set; } = DefaultAccentColor;

    /// <summary>Tijdstip van aanmaak (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
