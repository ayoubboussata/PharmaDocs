using PharmaDocs.Api.DTOs.Organizations;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Onboarding van nieuwe klanten (tenants). Enkel de operator (SystemAdmin) roept dit aan.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Maakt een organisatie aan met haar eerste tenant-admin. De admin logt daarna zelf
    /// in (de tenant-claim in zijn JWT scoopt al zijn data naar deze organisatie).
    /// </summary>
    Task<OrganizationResponse> ProvisionAsync(CreateOrganizationRequest request, CancellationToken ct = default);

    /// <summary>Overzicht van alle organisaties (operator-overzicht).</summary>
    Task<IReadOnlyList<OrganizationResponse>> GetAllAsync(CancellationToken ct = default);
}
