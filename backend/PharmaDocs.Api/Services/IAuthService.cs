using PharmaDocs.Api.DTOs.Auth;

namespace PharmaDocs.Api.Services;

public interface IAuthService
{
    /// <summary>Maakt een account aan (enkel door een admin). Geeft geen token terug.</summary>
    Task<CreatedUserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
