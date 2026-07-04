namespace PharmaDocs.Api.Models.Enums;

/// <summary>
/// Rol van een gebruiker. Enkel een <see cref="Admin"/> mag nieuwe accounts aanmaken;
/// zo staat de registratie niet open voor de buitenwereld.
/// </summary>
public enum UserRole
{
    User,
    Admin,
}
