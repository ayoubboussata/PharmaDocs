namespace PharmaDocs.Api.Services;

/// <summary>
/// Eén door de AI-service teruggegeven stuk tekst met zijn embedding-vector.
/// De backend zet dit om naar een <see cref="Models.KnowledgeChunk"/> in pgvector.
/// </summary>
public sealed record EmbeddedChunk(int Index, string Content, float[] Embedding);
