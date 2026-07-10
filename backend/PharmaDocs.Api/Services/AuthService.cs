using PharmaDocs.Api.Common;
using PharmaDocs.Api.Common.Exceptions;
using PharmaDocs.Api.DTOs.Auth;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;
using PharmaDocs.Api.Repositories;

namespace PharmaDocs.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IOrganizationRepository _organizations;
    private readonly ITenantContext _tenant;

    public AuthService(
        IUserRepository users,
        ITokenService tokens,
        IOrganizationRepository organizations,
        ITenantContext tenant)
    {
        _users = users;
        _tokens = tokens;
        _organizations = organizations;
        _tenant = tenant;
    }

    public async Task<CreatedUserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.ExistsByEmailAsync(email, ct))
            throw new ConflictException("Er bestaat al een account met dit e-mailadres.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            // Nieuwe accounts horen bij de tenant van de admin die ze aanmaakt.
            TenantId = _tenant.TenantId,
            Email = email,
            // Enhanced = SHA-384 pre-hash, geen stille afkapping op 72 bytes.
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password, workFactor: 12),
            // Door een admin aangemaakte accounts zijn gewone gebruikers.
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user, ct);

        return new CreatedUserResponse(user.Email, user.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);

        // Bewust één generieke fout voor "e-mail onbekend" én "wachtwoord fout":
        // zo lekken we niet welke e-mailadressen bestaan.
        if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Ongeldige inloggegevens.");

        var organization = await _organizations.GetByIdAsync(user.TenantId, ct);
        var organizationName = organization?.Name ?? "de apotheek";

        var token = _tokens.CreateToken(user, organizationName);
        return new AuthResponse(token.Token, user.Email, user.Role.ToString(), organizationName, token.ExpiresAt);
    }
}
