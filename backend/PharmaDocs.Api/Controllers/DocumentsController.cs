using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaDocs.Api.DTOs.Documents;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Api.Controllers;

/// <summary>
/// Poort voor documenten. Blijft dun: valideert de route en delegeert naar de service.
/// Vereist authenticatie. (Upload/extractie volgt in Fase 2–5.)
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _service;

    public DocumentsController(IDocumentService service) => _service = service;

    /// <summary>Overzicht van alle verwerkte/geüploade documenten.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentSummaryDto>>> GetAll(CancellationToken ct)
    {
        var documents = await _service.GetAllAsync(ct);
        return Ok(documents);
    }

    /// <summary>Detail van één document, inclusief geëxtraheerde factuur en lijnitems.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var document = await _service.GetByIdAsync(id, ct);
        return document is null ? NotFound() : Ok(document);
    }
}
