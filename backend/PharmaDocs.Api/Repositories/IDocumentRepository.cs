using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

/// <summary>
/// Data-toegang voor documenten. De service praat met deze interface,
/// niet rechtstreeks met de DbContext (testbaar + duidelijke grens).
/// </summary>
public interface IDocumentRepository
{
    /// <summary>Alle documenten, nieuwste eerst, met hun (eventuele) extractie.</summary>
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Eén document met extractie + lijnitems, of null als het niet bestaat.</summary>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Zoals <see cref="GetByIdAsync"/>, maar <b>getrackt</b> zodat wijzigingen
    /// (handmatige correctie) bewaard kunnen worden.
    /// </summary>
    Task<Document?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Voegt een nieuw document toe en bewaart meteen.</summary>
    Task AddAsync(Document document, CancellationToken ct = default);

    /// <summary>Bewaart openstaande wijzigingen (bv. na het toevoegen van de extractie).</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
