namespace PharmaDocs.Api.Configuration;

/// <summary>
/// Instellingen voor de interne Python AI-service, gebonden aan de sectie "AiService".
/// De backend is de enige die deze service aanroept (orchestrator-patroon).
/// </summary>
public class AiServiceSettings
{
    public const string SectionName = "AiService";

    /// <summary>Basis-URL van de FastAPI-service, bv. "http://localhost:8000".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Time-out (seconden) voor een extractie-call; Claude kan traag zijn.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
