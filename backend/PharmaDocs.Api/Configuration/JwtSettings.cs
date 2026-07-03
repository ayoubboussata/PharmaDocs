namespace PharmaDocs.Api.Configuration;

/// <summary>
/// JWT-instellingen, gebonden aan de sectie "Jwt" in de configuratie.
/// De geheime <see cref="Key"/> hoort in user-secrets / omgevingsvariabelen, nooit in Git.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
