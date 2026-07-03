using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Services;

/// <summary>Resultaat van het aanmaken van een token.</summary>
public record TokenResult(string Token, DateTime ExpiresAt);

/// <summary>Maakt JWT-access-tokens aan voor geauthenticeerde gebruikers.</summary>
public interface ITokenService
{
    TokenResult CreateToken(User user);
}
