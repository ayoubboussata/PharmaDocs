using PharmaDocs.Api.Common.Mapping;
using PharmaDocs.Api.DTOs.Documents;
using PharmaDocs.Api.Repositories;

namespace PharmaDocs.Api.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;

    public DocumentService(IDocumentRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<DocumentSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var documents = await _repository.GetAllAsync(ct);
        return documents.Select(d => d.ToSummaryDto()).ToList();
    }

    public async Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var document = await _repository.GetByIdAsync(id, ct);
        return document?.ToDetailDto();
    }
}
