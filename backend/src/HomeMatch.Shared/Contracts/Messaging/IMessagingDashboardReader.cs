namespace HomeMatch.Shared.Contracts.Messaging;

public sealed record DashboardLeadFunnelStats(
    int TotalLeads,
    int ContactedLeads,
    int ViewingScheduledLeads,
    int ContractSignedLeads);

public interface IMessagingDashboardReader
{
    Task<DashboardLeadFunnelStats> GetLeadFunnelByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        CancellationToken cancellationToken = default);
}
