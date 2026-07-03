namespace PharmaDocs.Api.DTOs.Knowledge;

/// <summary>Resultaat van het indexeren van één procedure-document.</summary>
public record KnowledgeIngestResponse(
    string SourceName,
    int ChunkCount);
