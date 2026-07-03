using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaDocs.Api.DTOs.Knowledge;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Api.Controllers;

/// <summary>
/// De kennisbank voor de RAG-assistent: procedures indexeren en het overzicht
/// van geïndexeerde bronnen. Blijft dun en delegeert naar de service.
/// </summary>
[Authorize]
[ApiController]
[Route("api/knowledge")]
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _service;

    public KnowledgeController(IKnowledgeService service) => _service = service;

    /// <summary>
    /// Indexeert een procedure-PDF (chunk → embedding → opslag in pgvector).
    /// Bestaande stukken van dezelfde bron worden vervangen.
    /// </summary>
    [HttpPost("documents")]
    [ProducesResponseType(typeof(KnowledgeIngestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<KnowledgeIngestResponse>> Ingest(IFormFile file, CancellationToken ct)
    {
        var result = await _service.IngestAsync(file, ct);
        return CreatedAtAction(nameof(GetSources), result);
    }

    /// <summary>Overzicht van de geïndexeerde bronnen met hun aantal stukken.</summary>
    [HttpGet("sources")]
    [ProducesResponseType(typeof(IReadOnlyList<KnowledgeSourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KnowledgeSourceDto>>> GetSources(CancellationToken ct)
    {
        var sources = await _service.GetSourcesAsync(ct);
        return Ok(sources);
    }
}
