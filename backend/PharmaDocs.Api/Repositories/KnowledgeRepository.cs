using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.DTOs.Knowledge;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly AppDbContext _db;

    public KnowledgeRepository(AppDbContext db) => _db = db;

    public async Task ReplaceSourceAsync(
        string sourceName, IReadOnlyList<KnowledgeChunk> chunks, CancellationToken ct = default)
    {
        // Delete + insert samen in één transactie: valt de insert weg (bv. DB-fout),
        // dan rolt ook de delete terug en blijft de oude index intact.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.KnowledgeChunks
            .Where(c => c.SourceName == sourceName)
            .ExecuteDeleteAsync(ct);

        _db.KnowledgeChunks.AddRange(chunks);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
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

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        float[] queryEmbedding, int k, CancellationToken ct = default)
    {
        var target = new Vector(queryEmbedding);

        // pgvector doet de cosinus-afstand in de databank; enkel de top-k komt terug.
        return await _db.KnowledgeChunks
            .OrderBy(c => c.Embedding!.CosineDistance(target))
            .Take(k)
            .Select(c => new RetrievedChunk(
                c.SourceName, c.Content, c.Embedding!.CosineDistance(target)))
            .ToListAsync(ct);
    }
}
