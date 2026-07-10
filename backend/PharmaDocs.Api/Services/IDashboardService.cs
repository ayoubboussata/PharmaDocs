using PharmaDocs.Api.DTOs.Dashboard;

namespace PharmaDocs.Api.Services;

/// <summary>Levert de dashboard-samenvatting voor de tenant (apotheek).</summary>
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}
