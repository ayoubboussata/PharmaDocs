namespace PharmaDocs.Api.DTOs.Auth;

/// <summary>Antwoord na een geslaagde registratie of login.</summary>
public record AuthResponse(
    string Token,
    string Email,
    DateTime ExpiresAt);
