using PharmaDocs.Api.DTOs.Documents;
using PharmaDocs.Api.Models;

namespace PharmaDocs.Api.Common.Mapping;

/// <summary>
/// Vertaalt de EF-entiteiten naar DTO's. Bewust op één plek gehouden,
/// zodat de vorm van de API-output centraal beheerd wordt.
/// </summary>
public static class DocumentMapping
{
    public static DocumentSummaryDto ToSummaryDto(this Document document) => new(
        document.Id,
        document.FileName,
        document.Status.ToString(),
        document.UploadedAt,
        document.ExtractedInvoice?.SupplierName,
        document.ExtractedInvoice?.InvoiceNumber,
        document.ExtractedInvoice?.InvoiceDate,
        document.ExtractedInvoice?.TotalAmount,
        document.ExtractedInvoice?.Currency);

    public static DocumentDetailDto ToDetailDto(this Document document) => new(
        document.Id,
        document.FileName,
        document.ContentType,
        document.FileSizeBytes,
        document.Status.ToString(),
        document.UploadedAt,
        document.ErrorMessage,
        document.ExtractedInvoice?.ToDto());

    public static ExtractedInvoiceDto ToDto(this ExtractedInvoice invoice) => new(
        invoice.Id,
        invoice.SupplierName,
        invoice.InvoiceNumber,
        invoice.InvoiceDate,
        invoice.TotalAmount,
        invoice.Currency,
        invoice.LineItems.Select(l => l.ToDto()).ToList());

    public static InvoiceLineItemDto ToDto(this InvoiceLineItem line) => new(
        line.Id,
        line.Description,
        line.Quantity,
        line.UnitPrice,
        line.LineTotal);
}
