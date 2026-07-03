using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace PharmaDocs.Api.Models;

/// <summary>
/// Eén stuk (chunk) van een geïndexeerd procedure-document, met zijn embedding.
/// De vectoren voeden de RAG-kennisassistent (Fase 4): een vraag wordt met deze
/// stukken vergeleken via pgvector om de meest relevante context te vinden.
/// </summary>
public class KnowledgeChunk
{
    public Guid Id { get; set; }

    /// <summary>Bron (bestandsnaam van de procedure) waartoe dit stuk hoort.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Volgorde van het stuk binnen de bron (0-gebaseerd).</summary>
    public int ChunkIndex { get; set; }

    /// <summary>De tekst van het stuk (wordt als context naar Claude gestuurd).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>De embedding (1024 dimensies, Voyage) — pgvector-kolom.</summary>
    [Column(TypeName = "vector(1024)")]
    public Vector? Embedding { get; set; }

    /// <summary>Tijdstip van indexering (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
