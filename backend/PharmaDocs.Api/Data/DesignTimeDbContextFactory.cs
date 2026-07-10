using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PharmaDocs.Api.Data;

/// <summary>
/// Enkel voor de EF-tooling (<c>dotnet ef migrations add</c>). Hiermee hoeft de
/// tooling de volledige applicatie-startup (met de JWT-/AI-guards in Program.cs)
/// niet te draaien. Bij het <b>bouwen</b> van een migratie wordt er niet met de
/// databank verbonden, dus de connectiestring is enkel een placeholder (of komt uit
/// de omgevingsvariabele <c>ConnectionStrings__DefaultConnection</c>).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=pharmadocs;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, o => o.UseVector())
            .Options;

        return new AppDbContext(options);
    }
}
