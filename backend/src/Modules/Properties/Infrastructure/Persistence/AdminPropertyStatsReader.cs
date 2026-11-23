using HomeMatch.Shared.Contracts.Admin;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

internal sealed class AdminPropertyStatsReader(PropertiesDbContext db) : IAdminPropertyStatsReader
{
    public async Task<AdminPropertyStats> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken = default)
    {
        var totalProperties = await db.Properties.AsNoTracking().CountAsync(cancellationToken);
        var rooms = await db.Rooms.AsNoTracking()
            .Select(r => new { r.Status, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var totalRooms = rooms.Count;
        var availableRooms = rooms.Count(r => r.Status == "AVAILABLE");
        var rentedRooms = rooms.Count(r => r.Status == "RENTED");
        var suspendedRooms = rooms.Count(r => r.Status == "SUSPENDED");
        var newRoomsInPeriod = fromUtc.HasValue
            ? rooms.Count(r => r.CreatedAt >= fromUtc.Value)
            : totalRooms;

        return new AdminPropertyStats(
            totalProperties,
            totalRooms,
            availableRooms,
            rentedRooms,
            suspendedRooms,
            newRoomsInPeriod);
    }
}
