using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PharmaDocs.Api.DTOs.Auth;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Api.Controllers;

/// <summary>Registratie (admin-only) en login. Login geeft een JWT-access-token terug.</summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")] // remt brute-force op login
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>
    /// Maakt een nieuw account aan. Registratie staat bewust <b>niet</b> open voor de
    /// buitenwereld: enkel een ingelogde admin mag accounts aanmaken.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(CreatedUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedUserResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var created = await _auth.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Logt in met e-mail + wachtwoord.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await _auth.LoginAsync(request, ct);
        return Ok(response);
    }
}
