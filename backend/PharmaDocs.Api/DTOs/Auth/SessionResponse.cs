namespace PharmaDocs.Api.DTOs.Auth;

/// <summary>
/// Wie er is ingelogd — voor de front-end. Bevat <b>geen</b> token: dat zit in een
/// httpOnly-cookie (L1) en is voor JavaScript onbereikbaar.
/// </summary>
public record SessionResponse(string Email, string Role);
