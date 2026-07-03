using System.ComponentModel.DataAnnotations;

namespace PharmaDocs.Api.DTOs.Auth;

/// <summary>Inloggegevens.</summary>
public record LoginRequest(
    [Required, EmailAddress]
    string Email,

    [Required]
    string Password);
