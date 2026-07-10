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
        // De tenant-scoping gebeurt via de global query filter (AppDbContext).
        return await _db.Documents
            .AsNoTracking()
            .Include(d => d.ExtractedInvoice)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // De global query filter scoopt op tenant: een document van een andere tenant
        // geeft null → 404.
        return await _db.Documents
            .AsNoTracking()
            .Include(d => d.ExtractedInvoice)
                .ThenInclude(i => i!.LineItems)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<Document?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Bewust géén AsNoTracking: de entiteit wordt gewijzigd en bewaard.
        return await _db.Documents
            .Include(d => d.ExtractedInvoice)
                .ThenInclude(i => i!.LineItems)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        _db.Documents.Add(document);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Document document, CancellationToken ct = default)
    {
        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
