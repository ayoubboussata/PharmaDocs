using PharmaDocs.Api.DTOs.Dashboard;

namespace PharmaDocs.Api.Services;

/// <summary>Levert de dashboard-samenvatting voor één gebruiker.</summary>
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default);
}
