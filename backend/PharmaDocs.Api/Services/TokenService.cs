using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PharmaDocs.Api.Configuration;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public TokenResult CreateToken(User user, string organizationName)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // Tenant (apotheek) waartoe de gebruiker hoort — voedt de multi-tenant
            // isolatie (ITenantContext + EF global query filter).
            new Claim("tenant", user.TenantId.ToString()),
            // Naam van de apotheek — puur voor weergave in de front-end (/auth/me).
            new Claim("org", organizationName),
            // Rol als "role"-claim; Program.cs zet RoleClaimType hierop zodat
            // [Authorize(Roles = "Admin")] werkt.
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResult(tokenString, expiresAt);
    }
}
