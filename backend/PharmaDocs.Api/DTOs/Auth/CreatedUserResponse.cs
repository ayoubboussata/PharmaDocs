namespace PharmaDocs.Api.DTOs.Auth;

/// <summary>
/// Antwoord nadat een admin een account heeft aangemaakt. Bevat bewust géén token:
/// de admin blijft als zichzelf ingelogd; de nieuwe gebruiker logt later zelf in.
/// </summary>
public record CreatedUserResponse(string Email, string Role);
