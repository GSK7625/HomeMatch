using HomeMatch.Shared.Contracts.Properties;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class PropertyDashboardReader(PropertiesDbContext db) : IPropertyDashboardReader
{
    public async Task<IReadOnlyList<DashboardRoomInfo>> GetRoomsForDashboardAsync(
        long? landlordId,
        long? propertyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Rooms
            .AsNoTracking()
            .Include(r => r.Property)
            .AsQueryable();

        if (landlordId.HasValue)
        {
            query = query.Where(r => r.Property.LandlordId == landlordId.Value);
        }

        if (propertyId.HasValue)
        {
            query = query.Where(r => r.PropertyId == propertyId.Value);
        }

        return await query
            .OrderBy(r => r.PropertyId)
            .ThenBy(r => r.RoomNumber)
            .Select(r => new DashboardRoomInfo(
                r.Id,
                r.PropertyId,
                r.Property.Name,
                r.RoomNumber,
                r.Status))
            .ToListAsync(cancellationToken);
    }
}
