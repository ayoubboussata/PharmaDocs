namespace PharmaDocs.Api.Models.Enums;

/// <summary>
/// Rol van een gebruiker.
/// <list type="bullet">
/// <item><see cref="User"/> — gewone apotheekmedewerker.</item>
/// <item><see cref="Admin"/> — tenant-admin: beheert de accounts binnen de eigen apotheek.</item>
/// <item><see cref="SystemAdmin"/> — operator (SaaS-beheerder): maakt organisaties (tenants)
/// aan met hun eerste tenant-admin. Overkoepelt de tenants.</item>
/// </list>
/// </summary>
public enum UserRole
{
    User,
    Admin,
    SystemAdmin,
}
