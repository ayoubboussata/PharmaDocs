using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Data;

/// <summary>
/// EF Core DbContext — de brug tussen de C#-modellen en PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ExtractedInvoice> ExtractedInvoices => Set<ExtractedInvoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // pgvector-extensie: nodig voor het vector-kolomtype (RAG, Fase 4).
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();

            // Rol als leesbare string in de databank i.p.v. een int.
            entity.Property(u => u.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // E-mail moet uniek zijn (geen twee accounts op hetzelfde adres).
            entity.HasIndex(u => u.Email).IsUnique();
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

            // Snel de stukken van één bron ophalen/verwijderen bij herindexeren.
            entity.HasIndex(c => c.SourceName);
        });
    }
}
