using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Common;
using PharmaDocs.Api.Data;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;

namespace PharmaDocs.Tests;

/// <summary>
/// Kern van de multi-tenant isolatie (Fase 2): de EF global query filter zorgt dat
/// een tenant nooit de documenten of kennisstukken van een andere tenant ziet.
/// Draait op de InMemory-provider met twee tenant-contexts op dezelfde store.
/// </summary>
public class TenantIsolationTests
{
    private sealed class StubTenant : ITenantContext
    {
        public StubTenant(Guid id) => TenantId = id;
        public Guid TenantId { get; }
    }

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static AppDbContext ContextFor(Guid tenant, string store)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(store)
            .Options;
        return new AppDbContext(options, new StubTenant(tenant));
    }

    private static Document Doc(Guid tenant, string fileName) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant,
        UserId = Guid.NewGuid(),
        FileName = fileName,
        ContentType = "application/pdf",
        FileSizeBytes = 100,
        UploadedAt = DateTime.UtcNow,
        Status = DocumentStatus.Pending,
    };

    private static KnowledgeChunk Chunk(Guid tenant, string source) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant,
        SourceName = source,
        ChunkIndex = 0,
        Content = "inhoud",
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Facturen_zijn_geisoleerd_per_tenant()
    {
        var store = nameof(Facturen_zijn_geisoleerd_per_tenant);
        using (var seed = ContextFor(TenantA, store))
        {
            seed.Documents.Add(Doc(TenantA, "a.pdf"));
            seed.Documents.Add(Doc(TenantB, "b.pdf"));
            await seed.SaveChangesAsync();
        }

        using var ctxA = ContextFor(TenantA, store);
        var forA = await ctxA.Documents.ToListAsync();
        Assert.Single(forA);
        Assert.Equal("a.pdf", forA[0].FileName);

        using var ctxB = ContextFor(TenantB, store);
        var forB = await ctxB.Documents.ToListAsync();
        Assert.Single(forB);
        Assert.Equal("b.pdf", forB[0].FileName);

        // Kruislings ophalen op id geeft niets → in de service wordt dat een 404.
        var aId = forA[0].Id;
        Assert.Null(await ctxB.Documents.FirstOrDefaultAsync(d => d.Id == aId));
    }

    [Fact]
    public async Task Kennisstukken_zijn_geisoleerd_per_tenant()
    {
        var store = nameof(Kennisstukken_zijn_geisoleerd_per_tenant);
        using (var seed = ContextFor(TenantA, store))
        {
            seed.KnowledgeChunks.Add(Chunk(TenantA, "procedure-a.pdf"));
            seed.KnowledgeChunks.Add(Chunk(TenantB, "procedure-b.pdf"));
            await seed.SaveChangesAsync();
        }

        // De RAG-zoektocht (SearchAsync) en het bronnenoverzicht draaien op dezelfde
        // DbSet → dezelfde filter. Tenant A mag procedure-b nooit terugvinden.
        using var ctxA = ContextFor(TenantA, store);
        var forA = await ctxA.KnowledgeChunks.ToListAsync();
        Assert.Single(forA);
        Assert.Equal("procedure-a.pdf", forA[0].SourceName);
        Assert.DoesNotContain(forA, c => c.SourceName == "procedure-b.pdf");

        using var ctxB = ContextFor(TenantB, store);
        var forB = await ctxB.KnowledgeChunks.ToListAsync();
        Assert.Single(forB);
        Assert.Equal("procedure-b.pdf", forB[0].SourceName);
    }
}
