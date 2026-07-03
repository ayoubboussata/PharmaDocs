using System.ComponentModel.DataAnnotations;

namespace PharmaDocs.Api.DTOs.Auth;

/// <summary>Gegevens om een nieuw account aan te maken.</summary>
public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)]
    string Email,

    [Required, MinLength(8), MaxLength(128)]
    string Password);
