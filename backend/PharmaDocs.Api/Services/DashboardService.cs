using PharmaDocs.Api.DTOs.Dashboard;
using PharmaDocs.Api.Models;
using PharmaDocs.Api.Models.Enums;
using PharmaDocs.Api.Repositories;

namespace PharmaDocs.Api.Services;

/// <summary>
/// Berekent de dashboard-cijfers uit de verwerkte facturen van de gebruiker.
/// De aggregatie gebeurt in-memory op de (per gebruiker gefilterde) documenten —
/// eenvoudig en ruim voldoende voor het datavolume van deze toepassing.
/// </summary>
public class DashboardService : IDashboardService
{
    private const int TopSuppliers = 8;

    private readonly IDocumentRepository _repository;

    public DashboardService(IDocumentRepository repository) => _repository = repository;

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var documents = await _repository.GetAllAsync(userId, ct);

        // Enkel verwerkte documenten met een extractie tellen mee.
        var invoices = documents
            .Where(d => d.Status == DocumentStatus.Processed && d.ExtractedInvoice is not null)
            .Select(d => d.ExtractedInvoice!)
            .ToList();

        var totalSpend = invoices.Sum(i => i.TotalAmount);
        var currency = invoices
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Currency) ? "EUR" : i.Currency)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "EUR";

        var bySupplier = invoices
            .GroupBy(i => string.IsNullOrWhiteSpace(i.SupplierName) ? "Onbekend" : i.SupplierName)
            .Select(g => new SpendByLabelDto(g.Key, g.Sum(i => i.TotalAmount), g.Count()))
            .OrderByDescending(s => s.Total)
            .Take(TopSuppliers)
            .ToList();

        var byCategory = invoices
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Category) ? "Niet gecategoriseerd" : i.Category!)
            .Select(g => new SpendByLabelDto(g.Key, g.Sum(i => i.TotalAmount), g.Count()))
            .OrderByDescending(s => s.Total)
            .ToList();

        var byMonth = invoices
            .Where(i => i.InvoiceDate is not null)
            .GroupBy(i => i.InvoiceDate!.Value.ToString("yyyy-MM"))
            .Select(g => new SpendByMonthDto(g.Key, g.Sum(i => i.TotalAmount), g.Count()))
            .OrderBy(m => m.Month)
            .ToList();

        return new DashboardSummaryDto(
            totalSpend,
            invoices.Count,
            currency,
            bySupplier,
            byMonth,
            byCategory);
    }
}
