using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.DTOs.Knowledge;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly AppDbContext _db;

    public KnowledgeRepository(AppDbContext db) => _db = db;

    public async Task DeleteBySourceAsync(string sourceName, CancellationToken ct = default)
    {
        await _db.KnowledgeChunks
            .Where(c => c.SourceName == sourceName)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct = default)
    {
        _db.KnowledgeChunks.AddRange(chunks);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<KnowledgeSourceDto>> GetSourcesAsync(CancellationToken ct = default)
    {
        // Projectie zonder de (grote) vectorkolom: enkel groeperen per bron.
        // Sorteren gebeurt in geheugen (een OrderBy op de aggregatie is niet
        // vertaalbaar, en het aantal bronnen is klein).
        var sources = await _db.KnowledgeChunks
            .GroupBy(c => c.SourceName)
            .Select(g => new KnowledgeSourceDto(g.Key, g.Count(), g.Max(c => c.CreatedAt)))
            .ToListAsync(ct);

        return sources.OrderByDescending(s => s.IndexedAt).ToList();
    }
}
