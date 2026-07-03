using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;

    public DocumentRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default)
    {
        // AsNoTracking: read-only query, geen change-tracking nodig → sneller.
        return await _db.Documents
            .AsNoTracking()
            .Include(d => d.ExtractedInvoice)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Documents
            .AsNoTracking()
            .Include(d => d.ExtractedInvoice)
                .ThenInclude(i => i!.LineItems)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }
}
