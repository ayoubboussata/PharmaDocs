using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaDocs.Api.DTOs.Organizations;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Api.Controllers;

/// <summary>
/// Onboarding van klanten (tenants). Enkel een <b>operator</b> (SystemAdmin) mag
/// organisaties aanmaken en overzien — dit staat boven de tenants en heeft dus geen
/// tenant-scope. De aangemaakte tenant-admin beheert daarna zelf zijn gebruikers via
/// <c>POST /api/auth/register</c> (binnen zijn eigen tenant).
/// </summary>
[Authorize(Roles = "SystemAdmin")]
[ApiController]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _service;

    public OrganizationsController(IOrganizationService service) => _service = service;

    /// <summary>Maakt een organisatie aan met haar eerste tenant-admin.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationResponse>> Create(
        CreateOrganizationRequest request, CancellationToken ct)
    {
        var organization = await _service.ProvisionAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, organization);
    }

    /// <summary>Overzicht van alle organisaties.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizationResponse>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));
}
