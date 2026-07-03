namespace PharmaDocs.Api.DTOs.Knowledge;

/// <summary>Een geïndexeerde bron (procedure) met het aantal stukken.</summary>
public record KnowledgeSourceDto(
    string SourceName,
    int ChunkCount,
    DateTime IndexedAt);
