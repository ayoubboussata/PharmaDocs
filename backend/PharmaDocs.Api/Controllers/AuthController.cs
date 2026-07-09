using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PharmaDocs.Api.DTOs.Auth;
using PharmaDocs.Api.Services;

namespace PharmaDocs.Api.Controllers;

/// <summary>
/// Registratie (admin-only), login, logout en de huidige sessie.
/// Login zet het JWT in een <b>httpOnly-cookie</b> (L1) i.p.v. het in de body terug
/// te geven — zo kan JavaScript (en dus een XSS-aanval) er niet bij.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")] // remt brute-force op login
public class AuthController : ControllerBase
{
    /// <summary>Naam van de httpOnly-cookie met het access-token.</summary>
    public const string AuthCookie = "access_token";

    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService auth, IWebHostEnvironment env)
    {
        _auth = auth;
        _env = env;
    }

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

    /// <summary>Logt in met e-mail + wachtwoord. Zet het token als httpOnly-cookie.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var auth = await _auth.LoginAsync(request, ct);
        SetAuthCookie(auth.Token, auth.ExpiresAt);
        return Ok(new SessionResponse(auth.Email, auth.Role));
    }

    /// <summary>Wie is er ingelogd (op basis van de cookie)? Voor de front-end bij het opstarten.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<SessionResponse> Me()
    {
        var email = User.FindFirst("email")?.Value ?? string.Empty;
        var role = User.FindFirst("role")?.Value ?? "User";
        return Ok(new SessionResponse(email, role));
    }

    /// <summary>Meldt af door de auth-cookie te wissen. Werkt ook met een verlopen sessie.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
        });
        return NoContent();
    }

    // httpOnly + SameSite=Strict = niet leesbaar voor JS én niet meegestuurd bij
    // cross-site requests → beschermt tegen XSS-tokendiefstal en CSRF. Secure enkel
    // buiten Development (lokaal draait de proxy over http).
    private void SetAuthCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(AuthCookie, token, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
        });
    }
}
