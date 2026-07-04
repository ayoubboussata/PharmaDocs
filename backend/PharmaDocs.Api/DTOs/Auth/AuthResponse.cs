namespace PharmaDocs.Api.DTOs.Auth;

/// <summary>Antwoord na een geslaagde login (bevat het access-token en de rol).</summary>
public record AuthResponse(
    string Token,
    string Email,
    string Role,
    DateTime ExpiresAt);
