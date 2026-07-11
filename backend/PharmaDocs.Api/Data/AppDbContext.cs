using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PharmaDocs.Api.Common;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Data;

/// <summary>
/// EF Core DbContext — de brug tussen de C#-modellen en PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    // De tenant van het huidige verzoek. Wordt door de global query filters gebruikt
    // zodat élke query automatisch tenant-gescoped is (multi-tenant isolatie, Fase 2).
    private readonly Guid _tenantId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options)
        => _tenantId = tenant.TenantId;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ExtractedInvoice> ExtractedInvoices => Set<ExtractedInvoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // ExtractedInvoice/InvoiceLineItem worden uitsluitend via hun (tenant-gefilterde)
        // Document geladen en hebben zelf geen query filter. Onderdruk de EF-waarschuwing
        // die anders bij elke query over die "required navigation"-interactie zou loggen.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // pgvector-extensie: nodig voor het vector-kolomtype (RAG, Fase 4).
        modelBuilder.HasPostgresExtension("vector");

        // --- Multi-tenant: elke tenant is één apotheekbedrijf (Fase 1) ---
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
            entity.Property(o => o.Slug).IsRequired().HasMaxLength(120);
            entity.Property(o => o.AccentColor).IsRequired().HasMaxLength(9)
                  .HasDefaultValue(Organization.DefaultAccentColor);
            entity.HasIndex(o => o.Slug).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();

            // Rol als leesbare string in de databank i.p.v. een int.
            entity.Property(u => u.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // E-mail globaal uniek: één persoon = één account (beslissing 2026-07-10).
            entity.HasIndex(u => u.Email).IsUnique();

            // Tenant-koppeling. Restrict: een organisatie met gebruikers kan niet
            // zomaar verwijderd worden (tenant-offboarding is een expliciete stap, Fase 6).
            entity.HasIndex(u => u.TenantId);
            entity.HasOne<Organization>()
                  .WithMany()
                  .HasForeignKey(u => u.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(d => d.FileName).IsRequired().HasMaxLength(500);
            entity.Property(d => d.ContentType).IsRequired().HasMaxLength(100);

            // Enum als leesbare string in de databank i.p.v. een int.
            entity.Property(d => d.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Eén Document ── één ExtractedInvoice.
            // Verwijder je een Document, dan verdwijnt de bijhorende extractie mee (cascade).
            entity.HasOne(d => d.ExtractedInvoice)
                  .WithOne(i => i.Document)
                  .HasForeignKey<ExtractedInvoice>(i => i.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Eigenaar (uploader). FK naar Users; EF legt automatisch een index op UserId.
            // Cascade: verdwijnt de gebruiker, dan ook zijn documenten.
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Tenant-koppeling (de facturen zijn gedeeld binnen de apotheek).
            entity.HasIndex(d => d.TenantId);
            entity.HasOne<Organization>()
                  .WithMany()
                  .HasForeignKey(d => d.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Global query filter: élke query op Documents ziet enkel de eigen tenant.
            // Isolatie is zo de default, niet iets dat je per query moet onthouden.
            entity.HasQueryFilter(d => d.TenantId == _tenantId);
        });

        modelBuilder.Entity<ExtractedInvoice>(entity =>
        {
            entity.Property(i => i.SupplierName).IsRequired().HasMaxLength(300);
            entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(100);
            entity.Property(i => i.Currency).IsRequired().HasMaxLength(3);
            entity.Property(i => i.Category).HasMaxLength(50);
            entity.Property(i => i.SubtotalAmount).HasPrecision(18, 2);
            entity.Property(i => i.VatRate).HasPrecision(5, 2);
            entity.Property(i => i.VatAmount).HasPrecision(18, 2);
            entity.Property(i => i.TotalAmount).HasPrecision(18, 2);

            // Uniek: precies één extractie per document.
            entity.HasIndex(i => i.DocumentId).IsUnique();

            // Eén factuur ── veel lijnitems (cascade delete).
            entity.HasMany(i => i.LineItems)
                  .WithOne(l => l.ExtractedInvoice)
                  .HasForeignKey(l => l.ExtractedInvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.Property(l => l.Description).IsRequired().HasMaxLength(500);
            entity.Property(l => l.Quantity).HasPrecision(18, 3);
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
            entity.Property(l => l.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<KnowledgeChunk>(entity =>
        {
            entity.Property(c => c.SourceName).IsRequired().HasMaxLength(500);
            entity.Property(c => c.Content).IsRequired();

            // De pgvector-kolom bestaat enkel op PostgreSQL. Onder een niet-relationele
            // provider (InMemory, in de tests) kent EF het Vector-type niet → negeer de
            // property daar. Het schema op Npgsql blijft ongewijzigd (vector(1024)).
            if (!Database.IsNpgsql())
                entity.Ignore(c => c.Embedding);

            // Snel de stukken van één bron binnen een tenant ophalen/verwijderen bij
            // herindexeren. Tenant-scoped zodat "openingsuren.pdf" van twee apotheken
            // niet botst (Fase 2 dwingt de tenant-filter af op de query's zelf).
            entity.HasIndex(c => new { c.TenantId, c.SourceName });
            entity.HasOne<Organization>()
                  .WithMany()
                  .HasForeignKey(c => c.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Global query filter: de RAG-zoektocht (SearchAsync) en het bronnenoverzicht
            // zien enkel de kennisstukken van de eigen tenant → sluit het cross-tenant lek.
            entity.HasQueryFilter(c => c.TenantId == _tenantId);
        });
    }
}
