namespace PharmaDocs.Api.DTOs.Dashboard;

/// <summary>
/// Samenvattende cijfers over de verwerkte facturen van één gebruiker,
/// voor het dashboard: totalen + uitsplitsingen per leverancier, maand en categorie.
/// </summary>
public record DashboardSummaryDto(
    decimal TotalSpend,
    int InvoiceCount,
    string Currency,
    IReadOnlyList<SpendByLabelDto> BySupplier,
    IReadOnlyList<SpendByMonthDto> ByMonth,
    IReadOnlyList<SpendByLabelDto> ByCategory);

/// <summary>Uitgaven gegroepeerd per label (leverancier of categorie).</summary>
public record SpendByLabelDto(string Label, decimal Total, int Count);

/// <summary>Uitgaven per maand (label als "YYYY-MM", chronologisch).</summary>
public record SpendByMonthDto(string Month, decimal Total, int Count);
