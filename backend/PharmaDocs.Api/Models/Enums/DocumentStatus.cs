namespace PharmaDocs.Api.Models.Enums;

/// <summary>
/// Verwerkingsstatus van een geüpload document.
/// Wordt als string in de databank opgeslagen (leesbaar + stabiel bij herordening).
/// </summary>
public enum DocumentStatus
{
    /// <summary>Geüpload, wacht op AI-extractie (Fase 2).</summary>
    Pending,

    /// <summary>AI-extractie geslaagd; er is een ExtractedInvoice.</summary>
    Processed,

    /// <summary>Extractie mislukt (bv. onleesbare PDF).</summary>
    Failed
}
