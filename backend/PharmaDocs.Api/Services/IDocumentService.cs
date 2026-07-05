using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.DTOs.Documents;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Bedrijfslogica rond documenten. De controller roept enkel deze service aan.
/// </summary>
public interface IDocumentService
{
    Task<IReadOnlyList<DocumentSummaryDto>> GetAllAsync(Guid userId, CancellationToken ct = default);

    Task<DocumentDetailDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Ontvangt een geüploade PDF, laat de AI-service de factuur extraheren en
    /// bewaart het resultaat op naam van <paramref name="userId"/>. Bij een mislukte
    /// extractie wordt het document als <c>Failed</c> vastgelegd (upload gaat nooit verloren).
    /// </summary>
    Task<DocumentDetailDto> UploadAndExtractAsync(IFormFile file, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Handmatige correctie van de geëxtraheerde factuur. Geeft het bijgewerkte
    /// document terug, of <c>null</c> als het niet bestaat of niet van deze gebruiker is.
    /// </summary>
    Task<DocumentDetailDto?> UpdateInvoiceAsync(Guid id, Guid userId, UpdateInvoiceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Bouwt een CSV-export (UTF-8 met BOM, één rij per document) van alle facturen
    /// van <paramref name="userId"/>, klaar om in Excel te openen.
    /// </summary>
    Task<byte[]> ExportCsvAsync(Guid userId, CancellationToken ct = default);
}
