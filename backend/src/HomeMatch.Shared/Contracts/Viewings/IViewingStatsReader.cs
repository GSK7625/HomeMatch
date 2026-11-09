namespace HomeMatch.Shared.Contracts.Viewings;

public interface IViewingStatsReader
{
    Task<int> GetTotalAppointmentsCountByTenantAsync(long tenantId, CancellationToken cancellationToken = default);
}
