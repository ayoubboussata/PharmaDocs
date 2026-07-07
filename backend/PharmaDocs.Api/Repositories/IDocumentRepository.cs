using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

/// <summary>
/// Data-toegang voor documenten. De service praat met deze interface,
/// niet rechtstreeks met de DbContext (testbaar + duidelijke grens).
/// </summary>
public interface IDocumentRepository
{
    /// <summary>Alle documenten <b>van deze gebruiker</b>, nieuwste eerst, met hun extractie.</summary>
    Task<IReadOnlyList<Document>> GetAllAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Eén document van deze gebruiker met extractie + lijnitems, of null als het
    /// niet bestaat <b>of niet van deze gebruiker is</b> (eigenaarschapscheck).
    /// </summary>
    Task<Document?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Zoals <see cref="GetByIdAsync"/>, maar <b>getrackt</b> zodat wijzigingen
    /// (handmatige correctie) bewaard kunnen worden. Ook hier de eigenaarschapscheck.
    /// </summary>
    Task<Document?> GetTrackedByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>Voegt een nieuw document toe en bewaart meteen.</summary>
    Task AddAsync(Document document, CancellationToken ct = default);

    /// <summary>Verwijdert een document (en via cascade de extractie) en bewaart meteen.</summary>
    Task DeleteAsync(Document document, CancellationToken ct = default);

    /// <summary>Bewaart openstaande wijzigingen (bv. na het toevoegen van de extractie).</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
