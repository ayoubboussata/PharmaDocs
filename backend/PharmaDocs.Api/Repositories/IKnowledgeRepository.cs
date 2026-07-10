using PharmaDocs.Api.DTOs.Knowledge;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

/// <summary>Een via vector-similariteit opgehaald stuk, met zijn afstand tot de vraag.</summary>
public sealed record RetrievedChunk(string SourceName, string Content, double Distance);

/// <summary>Data-toegang voor de RAG-kennisstukken (pgvector).</summary>
public interface IKnowledgeRepository
{
    /// <summary>
    /// Vervangt alle stukken van een bron door de nieuwe set, in één transactie
    /// (herindexeren): eerst de oude weg, dan de nieuwe erin. Zo kan een herindexering
    /// nooit half slagen en de bron leeg achterlaten.
    /// </summary>
    Task ReplaceSourceAsync(
        string sourceName, IReadOnlyList<KnowledgeChunk> chunks, CancellationToken ct = default);

    /// <summary>Overzicht van geïndexeerde bronnen (zonder de vectoren te laden).</summary>
    Task<IReadOnlyList<KnowledgeSourceDto>> GetSourcesAsync(CancellationToken ct = default);

    /// <summary>De <paramref name="k"/> stukken die het dichtst bij de vraagvector liggen (cosinus).</summary>
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        float[] queryEmbedding, int k, CancellationToken ct = default);
}
