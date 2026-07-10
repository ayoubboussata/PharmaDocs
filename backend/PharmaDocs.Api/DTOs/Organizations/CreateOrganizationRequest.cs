using System.ComponentModel.DataAnnotations;

namespace PharmaDocs.Api.DTOs.Organizations;

/// <summary>
/// Gegevens om een nieuwe organisatie (apotheek/tenant) aan te maken, samen met haar
/// eerste tenant-admin. Enkel een operator (SystemAdmin) mag dit.
/// </summary>
public record CreateOrganizationRequest(
    [Required, MaxLength(200)]
    string Name,

    // Optioneel; wordt afgeleid uit de naam als hij leeg is.
    [MaxLength(120)]
    string? Slug,

    [Required, EmailAddress, MaxLength(256)]
    string AdminEmail,

    [Required, MinLength(8), MaxLength(128)]
    string AdminPassword);
