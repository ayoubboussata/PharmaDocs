namespace PharmaDocs.Api.Services;

/// <summary>
/// Client naar de Python AI-service voor embeddings (<c>POST /embed-document</c>).
/// </summary>
public interface IEmbeddingClient
{
    /// <summary>
    /// Stuurt een procedure-PDF door; de AI-service extraheert de tekst, knipt ze in
    /// stukken en berekent per stuk een embedding. Gooit
    /// <see cref="Common.Exceptions.ServiceUnavailableException"/> als de service of
    /// Voyage niet beschikbaar is, en <see cref="Common.Exceptions.BadRequestException"/>
    /// bij een onleesbare PDF.
    /// </summary>
    Task<IReadOnlyList<EmbeddedChunk>> EmbedDocumentAsync(
        Stream pdfStream, string fileName, string contentType, CancellationToken ct = default);
}
