using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PharmaDocs.Api.Common;
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
        var documents = await _service.GetAllAsync(User.GetUserId(), ct);
        return Ok(documents);
    }

    /// <summary>Detail van één document, inclusief geëxtraheerde factuur en lijnitems.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var document = await _service.GetByIdAsync(id, User.GetUserId(), ct);
        return document is null ? NotFound() : Ok(document);
    }

    /// <summary>
    /// Uploadt een factuur-PDF. De backend roept intern de Python AI-service aan,
    /// bewaart het geëxtraheerde resultaat en geeft het document terug.
    /// Een mislukte extractie levert een document met status <c>Failed</c> (geen fout).
    /// </summary>
    [HttpPost("upload")]
    [EnableRateLimiting("ai")] // dure AI-extractie (Claude) → per gebruiker begrenzen
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // geen factuur herkend
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<DocumentDetailDto>> Upload(IFormFile file, CancellationToken ct)
    {
        var document = await _service.UploadAndExtractAsync(file, User.GetUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    /// <summary>
    /// Exporteert facturen als CSV (opent in Excel). Met een lijst <c>ids</c> in de
    /// body exporteer je enkel die documenten; zonder (of leeg) exporteer je alles.
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromBody] ExportInvoicesRequest? request, CancellationToken ct)
    {
        var csv = await _service.ExportCsvAsync(User.GetUserId(), request?.Ids, ct);
        return File(csv, "text/csv", $"pharmadocs-facturen-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    /// <summary>Verwijdert een document van de gebruiker (incl. de extractie via cascade).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, User.GetUserId(), ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Handmatige correctie van de geëxtraheerde factuur (kop + lijnitems).
    /// De meegestuurde lijnitems vervangen de bestaande volledig.
    /// </summary>
    [HttpPut("{id:guid}/invoice")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailDto>> UpdateInvoice(
        Guid id, UpdateInvoiceRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateInvoiceAsync(id, User.GetUserId(), request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}
