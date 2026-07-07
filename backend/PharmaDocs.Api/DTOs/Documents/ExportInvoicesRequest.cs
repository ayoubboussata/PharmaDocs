namespace PharmaDocs.Api.DTOs.Documents;

/// <summary>
/// Optionele selectie voor de CSV-export. Zijn er <see cref="Ids"/> meegegeven,
/// dan worden enkel die documenten geëxporteerd; anders alles.
/// </summary>
public record ExportInvoicesRequest(IReadOnlyList<Guid>? Ids);
