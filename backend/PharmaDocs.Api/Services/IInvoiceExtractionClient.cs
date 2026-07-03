namespace PharmaDocs.Api.Services;

/// <summary>
/// Client naar de interne Python AI-service (<c>POST /extract-invoice</c>).
/// De backend is de enige die deze service aanspreekt.
/// </summary>
public interface IInvoiceExtractionClient
{
    /// <summary>
    /// Stuurt de PDF door naar de AI-service en geeft de geëxtraheerde factuur terug.
    /// Gooit <see cref="AiExtractionException"/> als de service onbereikbaar is,
    /// een fout teruggeeft of de PDF niet verwerkt kan worden.
    /// </summary>
    Task<InvoiceExtractionResult> ExtractAsync(
        Stream pdfStream, string fileName, string contentType, CancellationToken ct = default);
}
