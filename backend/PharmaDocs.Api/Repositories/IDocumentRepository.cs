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
}
