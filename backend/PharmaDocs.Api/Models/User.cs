namespace PharmaDocs.Api.Models;

/// <summary>
/// Een gebruiker van de applicatie (apotheekmedewerker).
/// Wachtwoorden worden nooit in klare tekst bewaard, enkel de BCrypt-hash.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>E-mailadres, uniek — dient als login.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt-hash van het wachtwoord.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Tijdstip van registratie (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
