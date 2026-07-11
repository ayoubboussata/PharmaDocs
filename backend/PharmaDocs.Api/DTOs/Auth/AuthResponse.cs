namespace PharmaDocs.Api.DTOs.Auth;

/// <summary>Antwoord na een geslaagde login (bevat het access-token, de rol en de apotheek).</summary>
public record AuthResponse(
    string Token,
    string Email,
    string Role,
    string Organization,
    string OrganizationColor,
    DateTime ExpiresAt);
