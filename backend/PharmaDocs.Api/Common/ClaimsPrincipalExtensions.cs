using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace PharmaDocs.Api.Common;

/// <summary>Handige leesmethodes op de ingelogde gebruiker (uit het JWT).</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Haalt de user-id (claim "sub") uit het token. Gooit als hij ontbreekt/ongeldig is.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Geen geldige gebruikers-id in het token.");
    }
}
