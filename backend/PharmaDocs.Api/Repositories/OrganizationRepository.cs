using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _db;

    public OrganizationRepository(AppDbContext db) => _db = db;

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _db.Organizations.AnyAsync(o => o.Slug == slug, ct);

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Organization organization, CancellationToken ct = default)
    {
        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Organizations
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task DeleteCascadeAsync(Guid id, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Volgorde respecteert de Restrict-FK's naar Organizations (die als laatste weg).
        // IgnoreQueryFilters: de operator wist een ándere tenant dan zijn eigen, dus de
        // global query filter (op zijn tenant) mag hier niet gelden.
        // Facturen: de extractie en lijnitems verdwijnen mee via de DB-cascade.
        await _db.Documents.IgnoreQueryFilters().Where(d => d.TenantId == id).ExecuteDeleteAsync(ct);
        await _db.KnowledgeChunks.IgnoreQueryFilters().Where(c => c.TenantId == id).ExecuteDeleteAsync(ct);
        await _db.Users.Where(u => u.TenantId == id).ExecuteDeleteAsync(ct);
        await _db.Organizations.Where(o => o.Id == id).ExecuteDeleteAsync(ct);

        await tx.CommitAsync(ct);
    }
}
