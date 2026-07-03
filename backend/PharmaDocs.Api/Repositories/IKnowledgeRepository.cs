using PharmaDocs.Api.DTOs.Knowledge;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

/// <summary>Een via vector-similariteit opgehaald stuk, met zijn afstand tot de vraag.</summary>
public sealed record RetrievedChunk(string SourceName, string Content, double Distance);

/// <summary>Data-toegang voor de RAG-kennisstukken (pgvector).</summary>
public interface IKnowledgeRepository
{
    /// <summary>Verwijdert alle stukken van een bron (voor herindexeren).</summary>
    Task DeleteBySourceAsync(string sourceName, CancellationToken ct = default);

    /// <summary>Voegt stukken toe en bewaart.</summary>
    Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct = default);

    /// <summary>Overzicht van geïndexeerde bronnen (zonder de vectoren te laden).</summary>
    Task<IReadOnlyList<KnowledgeSourceDto>> GetSourcesAsync(CancellationToken ct = default);

    /// <summary>De <paramref name="k"/> stukken die het dichtst bij de vraagvector liggen (cosinus).</summary>
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        float[] queryEmbedding, int k, CancellationToken ct = default);
}
