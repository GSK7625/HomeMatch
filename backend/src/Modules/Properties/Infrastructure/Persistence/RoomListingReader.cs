using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class RoomListingReader(PropertiesDbContext db) : IRoomListingReader
{
    public Task<RoomListingSnapshot?> GetRoomAsync(long roomId, CancellationToken cancellationToken) =>
        db.Rooms.AsNoTracking()
            .Where(room => room.Id == roomId)
            .Select(room => new RoomListingSnapshot(
                room.Id, room.PropertyId, room.Property.LandlordId, room.Status,
                room.Property.ProvinceCode, room.Property.WardCode, room.Property.AddressDetail))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<long, RoomListingSnapshot>> GetRoomsAsync(
        IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken)
    {
        if (roomIds.Count == 0)
            return new Dictionary<long, RoomListingSnapshot>();

        var rooms = await db.Rooms.AsNoTracking()
            .Where(room => roomIds.Contains(room.Id))
            .Select(room => new RoomListingSnapshot(
                room.Id, room.PropertyId, room.Property.LandlordId, room.Status,
                room.Property.ProvinceCode, room.Property.WardCode, room.Property.AddressDetail))
            .ToListAsync(cancellationToken);
        return rooms.ToDictionary(room => room.RoomId);
    }

    public async Task<bool> HasElectricityAndWaterRatesAsync(
        long roomId, DateOnly date, CancellationToken cancellationToken)
    {
        var propertyId = await db.Rooms.AsNoTracking().Where(room => room.Id == roomId)
            .Select(room => (long?)room.PropertyId).FirstOrDefaultAsync(cancellationToken);
        if (propertyId is null)
            return false;

        var types = await db.UtilityTariffs.AsNoTracking()
            .Where(rate => rate.PropertyId == propertyId
                && rate.RoomId == null
                && rate.EffectiveFrom <= date
                && (rate.TariffType == "ELECTRICITY" || rate.TariffType == "WATER"))
            .Select(rate => rate.TariffType).Distinct().ToListAsync(cancellationToken);
        return types.Count == 2;
    }
}
