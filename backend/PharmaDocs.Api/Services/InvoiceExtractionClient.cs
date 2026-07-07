using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> naar de Python AI-service. Zet de PDF als
/// multipart-form door naar <c>POST /extract-invoice</c> en vertaalt het
/// JSON-antwoord naar een <see cref="InvoiceExtractionResult"/>.
/// </summary>
public class InvoiceExtractionClient : IInvoiceExtractionClient
{
    private readonly HttpClient _http;

    public InvoiceExtractionClient(HttpClient http) => _http = http;

    public async Task<InvoiceExtractionResult> ExtractAsync(
        Stream pdfStream, string fileName, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(pdfStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType);
        // Veldnaam "file" moet overeenkomen met de FastAPI-parameter.
        form.Add(fileContent, "file", fileName);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/extract-invoice", form, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Echte annulering door de client → laten doorlopen.
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Onbereikbaar of time-out (TaskCanceledException zonder ct = time-out).
            throw new AiExtractionException(
                "De AI-service is niet bereikbaar of reageerde niet op tijd.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadDetailAsync(response, ct);
            throw new AiExtractionException(
                $"AI-service gaf een fout ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ExtractInvoiceResponse>(ct)
            ?? throw new AiExtractionException("Leeg antwoord van de AI-service.");

        return payload.Invoice.ToResult();
    }

    /// <summary>Leest het <c>detail</c>-veld uit een FastAPI-foutantwoord (best effort).</summary>
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
            // Geen JSON-body of onverwacht formaat → val terug op de reason phrase.
        }
        return response.ReasonPhrase ?? "onbekende fout";
    }

    // --- Wire-DTO's: de exacte JSON-vorm van de Python-service ---

    private sealed record FastApiError(
        [property: JsonPropertyName("detail")] string? Detail);

    private sealed record ExtractInvoiceResponse(
        [property: JsonPropertyName("fileName")] string? FileName,
        [property: JsonPropertyName("pageCount")] int PageCount,
        [property: JsonPropertyName("invoice")] InvoicePayload Invoice);

    private sealed record InvoicePayload(
        [property: JsonPropertyName("supplierName")] string SupplierName,
        [property: JsonPropertyName("invoiceNumber")] string InvoiceNumber,
        [property: JsonPropertyName("invoiceDate")] string? InvoiceDate,
        [property: JsonPropertyName("subtotalAmount")] decimal SubtotalAmount,
        [property: JsonPropertyName("vatRate")] decimal? VatRate,
        [property: JsonPropertyName("vatAmount")] decimal VatAmount,
        [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("isInvoice")] bool IsInvoice,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("lineItems")] List<LineItemPayload> LineItems)
    {
        public InvoiceExtractionResult ToResult() => new(
            IsInvoice,
            SupplierName,
            InvoiceNumber,
            ParseDate(InvoiceDate),
            SubtotalAmount,
            VatRate,
            VatAmount,
            TotalAmount,
            string.IsNullOrWhiteSpace(Currency) ? "EUR" : Currency,
            string.IsNullOrWhiteSpace(Category) ? null : Category,
            (LineItems ?? new()).Select(l => l.ToResult()).ToList());

        // De AI levert de datum als ISO-string "YYYY-MM-DD" of null.
        private static DateOnly? ParseDate(string? value) =>
            DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d : null;
    }

    private sealed record LineItemPayload(
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("quantity")] decimal Quantity,
        [property: JsonPropertyName("unitPrice")] decimal UnitPrice,
        [property: JsonPropertyName("lineTotal")] decimal LineTotal)
    {
        public InvoiceLineResult ToResult() => new(Description, Quantity, UnitPrice, LineTotal);
    }
}
