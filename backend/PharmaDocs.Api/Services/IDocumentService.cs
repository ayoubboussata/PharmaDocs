using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.DTOs.Documents;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Bedrijfslogica rond documenten. De controller roept enkel deze service aan.
/// </summary>
public interface IDocumentService
{
    Task<IReadOnlyList<DocumentSummaryDto>> GetAllAsync(CancellationToken ct = default);

    Task<DocumentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Ontvangt een geüploade PDF, laat de AI-service de factuur extraheren en
    /// bewaart het resultaat. Bij een mislukte extractie wordt het document als
    /// <c>Failed</c> vastgelegd (de upload gaat nooit verloren).
    /// </summary>
    Task<DocumentDetailDto> UploadAndExtractAsync(IFormFile file, CancellationToken ct = default);

    /// <summary>
    /// Handmatige correctie van de geëxtraheerde factuur. Geeft het bijgewerkte
    /// document terug, of <c>null</c> als het document niet bestaat.
    /// </summary>
    Task<DocumentDetailDto?> UpdateInvoiceAsync(Guid id, UpdateInvoiceRequest request, CancellationToken ct = default);
}
