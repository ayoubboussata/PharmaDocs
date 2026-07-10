namespace PharmaDocs.Api.Services;

/// <summary>Eén stuk context (bron + tekst) dat als grond naar Claude gaat.</summary>
public sealed record RagContext(string SourceName, string Content);

/// <summary>
/// Client naar de AI-service voor het genereren van een gegrond RAG-antwoord
/// (<c>POST /answer</c>): vraag + opgehaalde fragmenten → antwoord van Claude.
/// </summary>
public interface IRagAnswerClient
{
    Task<string> AnswerAsync(
        string question, IReadOnlyList<RagContext> contexts, string organizationName,
        CancellationToken ct = default);
}
