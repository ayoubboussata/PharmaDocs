using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

/// <summary>
/// Data-toegang voor organisaties (tenants). Enkel de operator (SystemAdmin) beheert
/// deze; de tabel heeft geen tenant-filter (ze staat juist boven de tenants).
/// </summary>
public interface IOrganizationRepository
{
    /// <summary>Bestaat er al een organisatie met deze slug?</summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);

    /// <summary>De organisatie met deze id, of null.</summary>
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Voegt een organisatie toe en bewaart meteen.</summary>
    Task AddAsync(Organization organization, CancellationToken ct = default);

    /// <summary>Alle organisaties, nieuwste eerst.</summary>
    Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken ct = default);
}
