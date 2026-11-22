using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Modules.Messaging.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Messaging.Infrastructure.Services;

internal sealed class MessagingDashboardReader(MessagingDbContext db) : IMessagingDashboardReader
{
    public async Task<DashboardLeadFunnelStats> GetLeadFunnelByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
        {
            return new DashboardLeadFunnelStats(0, 0, 0, 0);
        }

        var idSet = listingIds.Distinct().ToList();

        var leadStatuses = await db.Leads
            .AsNoTracking()
            .Where(l => idSet.Contains(l.ListingId))
            .Select(l => l.Status)
            .ToListAsync(cancellationToken);

        var totalLeads = leadStatuses.Count;
        var contractSigned = leadStatuses.Count(s => s == LeadStatus.ContractSigned);
        var viewingScheduled = leadStatuses.Count(s =>
            s == LeadStatus.ViewingScheduled || s == LeadStatus.ContractSigned);
        var contacted = leadStatuses.Count(s =>
            s == LeadStatus.Contacted || s == LeadStatus.ViewingScheduled || s == LeadStatus.ContractSigned);

        return new DashboardLeadFunnelStats(
            TotalLeads: totalLeads,
            ContactedLeads: contacted,
            ViewingScheduledLeads: viewingScheduled,
            ContractSignedLeads: contractSigned);
    }
}
