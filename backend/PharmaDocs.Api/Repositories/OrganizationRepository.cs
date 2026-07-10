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
}
