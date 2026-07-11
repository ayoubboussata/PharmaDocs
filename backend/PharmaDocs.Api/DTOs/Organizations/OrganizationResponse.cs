namespace PharmaDocs.Api.DTOs.Organizations;

/// <summary>Een organisatie (apotheek/tenant) zoals teruggegeven aan de operator.</summary>
public record OrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    string AccentColor,
    DateTime CreatedAt);
