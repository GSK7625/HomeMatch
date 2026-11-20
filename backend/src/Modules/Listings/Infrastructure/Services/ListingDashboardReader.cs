using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class ListingDashboardReader(ListingsDbContext db) : IListingDashboardReader
{
    public async Task<IReadOnlyList<DashboardListingInfo>> GetListingsForDashboardAsync(
        long? landlordId,
        IReadOnlyCollection<long>? roomIds = null,
        CancellationToken cancellationToken = default)
    {
        if (roomIds is not null && roomIds.Count == 0)
        {
            return [];
        }

        var query = db.Listings.AsNoTracking().AsQueryable();

        if (landlordId.HasValue)
        {
            query = query.Where(l => l.LandlordId == landlordId.Value);
        }

        if (roomIds is not null)
        {
            query = query.Where(l => roomIds.Contains(l.RoomId));
        }

        return await query
            .Select(l => new DashboardListingInfo(
                l.Id,
                l.RoomId,
                l.LandlordId,
                l.Title,
                l.Status,
                l.ViewCount))
            .ToListAsync(cancellationToken);
    }
}
