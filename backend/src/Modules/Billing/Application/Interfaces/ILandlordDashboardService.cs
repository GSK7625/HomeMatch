using HomeMatch.Shared.Contracts.Billing;

namespace HomeMatch.Modules.Billing.Application.Interfaces;

public interface ILandlordDashboardService
{
    Task<LandlordDashboardDto> GetDashboardAsync(string? buildingId, string? period, CancellationToken cancellationToken = default);
}
