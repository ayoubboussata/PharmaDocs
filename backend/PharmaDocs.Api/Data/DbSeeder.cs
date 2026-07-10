using Microsoft.EntityFrameworkCore;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;

namespace PharmaDocs.Api.Data;

/// <summary>
/// Seedt bij het opstarten één admin-account uit de configuratie (sectie "Seed").
/// Nodig omdat registratie admin-only is: zonder een eerste admin kan niemand
/// accounts aanmaken. De sleutels horen in user-secrets / omgevingsvariabelen
/// (in productie een Container Apps-secret), nooit in Git.
/// </summary>
public static class DbSeeder
{
    public static void SeedAdmin(AppDbContext db, IConfiguration config, ILogger logger)
    {
        // Zorg dat de default-organisatie bestaat (idempotent): alle single-tenant
        // data hangt hieraan tot Fase 3 echte tenants introduceert. De migratie maakt
        // ze normaal al aan; dit is een veiligheidsnet.
        if (!db.Organizations.Any(o => o.Id == Organization.DefaultId))
        {
            db.Organizations.Add(new Organization
            {
                Id = Organization.DefaultId,
                Name = "Apotheek De Wit",
                Slug = "apotheek-de-wit",
                CreatedAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        var email = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Seed:AdminEmail/AdminPassword niet gezet — geen admin geseed. " +
                "Registratie blijft dicht tot er handmatig een Admin bestaat.");
            return;
        }

        // Al een admin? Dan niets doen (idempotent, geen wachtwoord overschrijven).
        if (db.Users.Any(u => u.Role == UserRole.Admin))
            return;

        // Bestaat het adres al als gewone gebruiker? Niet stilzwijgend promoveren.
        if (db.Users.Any(u => u.Email == email))
        {
            logger.LogWarning("Seed-admin {Email} bestaat al als gebruiker — niet aangepast.", email);
            return;
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = Organization.DefaultId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();
        logger.LogInformation("Admin-account geseed: {Email}", email);
    }
}
