using PharmaDocs.Api.Models.Enums;

namespace PharmaDocs.Api.Models;

/// <summary>
/// Een geüpload brondocument (factuur of bestelbon, PDF).
/// Bevat enkel de bestand-metadata; de geëxtraheerde inhoud zit in <see cref="ExtractedInvoice"/>.
/// </summary>
public class Document
{
    public Guid Id { get; set; }

    /// <summary>Oorspronkelijke bestandsnaam zoals geüpload.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME-type, bv. "application/pdf".</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Grootte van het bestand in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Tijdstip van upload (UTC).</summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>Waar de verwerking staat.</summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>Foutboodschap als <see cref="Status"/> == Failed (anders null).</summary>
    public string? ErrorMessage { get; set; }

    // --- Navigatie ---

    /// <summary>Eén-op-één met de geëxtraheerde factuur (null zolang niet verwerkt).</summary>
    public ExtractedInvoice? ExtractedInvoice { get; set; }
}
