using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Shared.Contracts.Viewings;

namespace HomeMatch.Modules.Viewings.Infrastructure.Services;

internal sealed class ViewingStatsReader : IViewingStatsReader
{
    private readonly IViewingAppointmentRepository _repository;

    public ViewingStatsReader(IViewingAppointmentRepository repository)
    {
        _repository = repository;
    }

    public Task<int> GetTotalAppointmentsCountByTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return _repository.CountByTenantAsync(tenantId, cancellationToken);
    }
}
