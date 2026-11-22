using HomeMatch.Modules.Viewings.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Viewings;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Viewings.Infrastructure.Services;

internal sealed class ViewingDashboardReader(ViewingsDbContext db) : IViewingDashboardReader
{
    public async Task<IReadOnlyList<DashboardViewingInfo>> GetViewingsByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
        {
            return [];
        }

        var idSet = listingIds.Distinct().ToList();

        return await db.ViewingAppointments
            .AsNoTracking()
            .Where(v => idSet.Contains(v.ListingId))
            .OrderBy(v => v.AppointmentTime)
            .Select(v => new DashboardViewingInfo(
                v.Id,
                v.ListingId,
                v.TenantId,
                v.AppointmentTime,
                v.VirtualViewing != null,
                v.Status))
            .ToListAsync(cancellationToken);
    }
}
