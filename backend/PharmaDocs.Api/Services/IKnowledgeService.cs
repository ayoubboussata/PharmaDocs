using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.DTOs.Knowledge;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Bedrijfslogica voor de RAG-kennisbank: procedures indexeren (chunk + embed +
/// opslaan) en het overzicht van geïndexeerde bronnen.
/// </summary>
public interface IKnowledgeService
{
    /// <summary>
    /// Indexeert een procedure-PDF: laat de AI-service ze in stukken knippen en
    /// embedden, en bewaart de vectoren. Bestaande stukken van dezelfde bron worden
    /// vervangen (herindexeren).
    /// </summary>
    Task<KnowledgeIngestResponse> IngestAsync(IFormFile file, CancellationToken ct = default);

    /// <summary>Overzicht van de geïndexeerde bronnen.</summary>
    Task<IReadOnlyList<KnowledgeSourceDto>> GetSourcesAsync(CancellationToken ct = default);

    /// <summary>
    /// Beantwoordt een vraag met RAG: query-embedding → dichtste stukken (pgvector) →
    /// gegrond antwoord van Claude met bronvermelding.
    /// </summary>
    Task<AskResponse> AskAsync(string question, CancellationToken ct = default);
}
