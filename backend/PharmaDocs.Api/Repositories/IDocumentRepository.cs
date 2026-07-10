using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

/// <summary>
/// Data-toegang voor documenten. De service praat met deze interface,
/// niet rechtstreeks met de DbContext (testbaar + duidelijke grens).
/// </summary>
public interface IDocumentRepository
{
    /// <summary>Alle documenten <b>van de tenant</b>, nieuwste eerst, met hun extractie.</summary>
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Eén document van de tenant met extractie + lijnitems, of null als het niet
    /// bestaat <b>of van een andere tenant is</b> (de global query filter scoopt op tenant).
    /// </summary>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Zoals <see cref="GetByIdAsync"/>, maar <b>getrackt</b> zodat wijzigingen
    /// (handmatige correctie) bewaard kunnen worden. Ook tenant-gescoped.
    /// </summary>
    Task<Document?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Voegt een nieuw document toe en bewaart meteen.</summary>
    Task AddAsync(Document document, CancellationToken ct = default);

    /// <summary>Verwijdert een document (en via cascade de extractie) en bewaart meteen.</summary>
    Task DeleteAsync(Document document, CancellationToken ct = default);

    /// <summary>Bewaart openstaande wijzigingen (bv. na het toevoegen van de extractie).</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
