namespace PharmaDocs.Api.Services;

/// <summary>
/// Fout bij het aanroepen van de Python AI-service (onbereikbaar, time-out,
/// onleesbare PDF, of AI niet beschikbaar). Wordt door de <see cref="DocumentService"/>
/// opgevangen en op het document als <c>Failed</c> + foutboodschap vastgelegd —
/// een upload gaat dus nooit verloren.
/// </summary>
public class AiExtractionException : Exception
{
    public AiExtractionException(string message) : base(message) { }
    public AiExtractionException(string message, Exception inner) : base(message, inner) { }
}
