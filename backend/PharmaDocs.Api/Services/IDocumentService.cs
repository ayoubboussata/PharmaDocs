using PharmaDocs.Api.DTOs.Documents;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Bedrijfslogica rond documenten. De controller roept enkel deze service aan.
/// (Upload/extractie komt later in Fase 2–5; voorlopig leesbewerkingen.)
/// </summary>
public interface IDocumentService
{
    Task<IReadOnlyList<DocumentSummaryDto>> GetAllAsync(CancellationToken ct = default);

    Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
