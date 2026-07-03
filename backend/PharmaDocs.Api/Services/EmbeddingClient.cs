using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PharmaDocs.Api.Common.Exceptions;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> naar de Python AI-service voor embeddings.
/// Stuurt de PDF als multipart naar <c>POST /embed-document</c> en geeft de
/// stukken met hun vectoren terug.
/// </summary>
public class EmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _http;

    public EmbeddingClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<EmbeddedChunk>> EmbedDocumentAsync(
        Stream pdfStream, string fileName, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(pdfStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType);
        form.Add(fileContent, "file", fileName);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/embed-document", form, ct);
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
            // 4xx = fout van de client (bv. onleesbare PDF) → 400; de rest → 503.
            if ((int)response.StatusCode is >= 400 and < 500
                && response.StatusCode != HttpStatusCode.ServiceUnavailable)
                throw new BadRequestException(detail);
            throw new ServiceUnavailableException(
                $"Embeddings zijn niet beschikbaar ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbedDocumentResponse>(ct)
            ?? throw new ServiceUnavailableException("Leeg antwoord van de AI-service.");

        return payload.Chunks
            .Select(c => new EmbeddedChunk(c.Index, c.Content, c.Embedding))
            .ToList();
    }

    public async Task<float[]> EmbedQueryAsync(string question, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/embed-query", new { text = question }, ct);
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
                $"Embeddings zijn niet beschikbaar ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbedQueryResponse>(ct)
            ?? throw new ServiceUnavailableException("Leeg antwoord van de AI-service.");
        return payload.Embedding;
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

    // --- Wire-DTO's: de JSON-vorm van de Python-service ---

    private sealed record FastApiError(
        [property: JsonPropertyName("detail")] string? Detail);

    private sealed record EmbedDocumentResponse(
        [property: JsonPropertyName("fileName")] string? FileName,
        [property: JsonPropertyName("chunks")] List<ChunkPayload> Chunks);

    private sealed record ChunkPayload(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("embedding")] float[] Embedding);

    private sealed record EmbedQueryResponse(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
