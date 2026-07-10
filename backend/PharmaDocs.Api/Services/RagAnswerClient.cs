using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PharmaDocs.Api.Common.Exceptions;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> naar de AI-service voor het RAG-antwoord.
/// Stuurt de vraag met de fragmenten naar <c>POST /answer</c> en geeft de tekst terug.
/// </summary>
public class RagAnswerClient : IRagAnswerClient
{
    private readonly HttpClient _http;

    public RagAnswerClient(HttpClient http) => _http = http;

    public async Task<string> AnswerAsync(
        string question, IReadOnlyList<RagContext> contexts, string organizationName,
        CancellationToken ct = default)
    {
        var body = new
        {
            question,
            organizationName,
            contexts = contexts.Select(c => new { sourceName = c.SourceName, content = c.Content }),
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/answer", body, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ServiceUnavailableException(
                "De AI-service is niet bereikbaar of reageerde niet op tijd.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadDetailAsync(response, ct);
            throw new ServiceUnavailableException(
                $"De kennisassistent is niet beschikbaar ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<AnswerResponse>(ct)
            ?? throw new ServiceUnavailableException("Leeg antwoord van de AI-service.");
        return payload.Answer;
    }

    private static async Task<string> ReadDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<FastApiError>(ct);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return problem!.Detail!;
        }
        catch
        {
            // Geen JSON-body → val terug op de reason phrase.
        }
        return response.ReasonPhrase ?? "onbekende fout";
    }

    private sealed record FastApiError(
        [property: JsonPropertyName("detail")] string? Detail);

    private sealed record AnswerResponse(
        [property: JsonPropertyName("answer")] string Answer);
}
