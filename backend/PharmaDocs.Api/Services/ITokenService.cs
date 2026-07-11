using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Services;

/// <summary>Resultaat van het aanmaken van een token.</summary>
public record TokenResult(string Token, DateTime ExpiresAt);

/// <summary>Maakt JWT-access-tokens aan voor geauthenticeerde gebruikers.</summary>
public interface ITokenService
{
    /// <summary>Maakt een token voor <paramref name="user"/>; de apotheeknaam en -accentkleur
    /// worden als "org"/"org_color"-claims opgenomen (voor de front-end, zonder extra DB-call op /me).</summary>
    TokenResult CreateToken(User user, string organizationName, string organizationColor);
}
