using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaDocs.Api.DTOs.Dashboard;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Api.Controllers;

/// <summary>
/// Dashboard-cijfers: totalen en uitsplitsingen (per leverancier, maand, categorie)
/// over de eigen verwerkte facturen. Blijft dun; delegeert naar de service.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    /// <summary>Samenvattende dashboard-cijfers voor de apotheek van de ingelogde gebruiker.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
    {
        var summary = await _service.GetSummaryAsync(ct);
        return Ok(summary);
    }
}
