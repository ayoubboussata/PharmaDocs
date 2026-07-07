using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;

    public DocumentRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Document>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        // AsNoTracking: read-only query, geen change-tracking nodig → sneller.
        return await _db.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Include(d => d.ExtractedInvoice)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task<Document?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        // Eigenaarschapscheck in de query: een vreemd document geeft null → 404.
        return await _db.Documents
            .AsNoTracking()
            .Include(d => d.ExtractedInvoice)
                .ThenInclude(i => i!.LineItems)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);
    }

    public async Task<Document?> GetTrackedByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        // Bewust géén AsNoTracking: de entiteit wordt gewijzigd en bewaard.
        return await _db.Documents
            .Include(d => d.ExtractedInvoice)
                .ThenInclude(i => i!.LineItems)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);
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
