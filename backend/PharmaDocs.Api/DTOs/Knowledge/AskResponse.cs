namespace PharmaDocs.Api.DTOs.Knowledge;

/// <summary>Het antwoord van de kennisassistent met de geraadpleegde bronnen.</summary>
public record AskResponse(
    string Answer,
    IReadOnlyList<string> Sources);
