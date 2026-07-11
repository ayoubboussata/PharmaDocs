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

    /// <summary>
    /// Seedt optioneel een <b>operator</b> (SystemAdmin) uit de sectie "Seed"
    /// (<c>OperatorEmail</c>/<c>OperatorPassword</c>). De operator maakt organisaties
    /// (tenants) aan met hun eerste tenant-admin. Niet gezet = geen operator geseed.
    /// <para>
    /// <b>Zelfhelend</b>: bestaat het operator-account al, dan wordt het op de seed-waarden
    /// gezet (rol = SystemAdmin, wachtwoord = de seed). Zo maakt een her-deploy een fout of
    /// verlopen operator-wachtwoord altijd terug goed — de seed is de bron van waarheid.
    /// </para>
    /// </summary>
    public static void SeedOperator(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var email = config["Seed:OperatorEmail"]?.Trim().ToLowerInvariant();
        var password = config["Seed:OperatorPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return; // operator is optioneel

        var passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12);

        var existing = db.Users.FirstOrDefault(u => u.Email == email);
        if (existing is not null)
        {
            // Zorg dat het account operator is én dat het wachtwoord met de seed matcht.
            existing.Role = UserRole.SystemAdmin;
            existing.PasswordHash = passwordHash;
            db.SaveChanges();
            logger.LogInformation("Operator (SystemAdmin) bijgewerkt uit de seed: {Email}", email);
            return;
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = Organization.DefaultId, // de operator woont in de default-organisatie
            Email = email,
            PasswordHash = passwordHash,
            Role = UserRole.SystemAdmin,
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();
        logger.LogInformation("Operator (SystemAdmin) geseed: {Email}", email);
    }
}
