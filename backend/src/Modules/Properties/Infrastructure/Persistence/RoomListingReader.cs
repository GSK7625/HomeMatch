using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class RoomListingReader(PropertiesDbContext db) : IRoomListingReader
{
    public async Task<RoomListingDetailSnapshot?> GetRoomDetailAsync(long roomId, CancellationToken cancellationToken)
    {
        var detail = await (
            from room in db.Rooms.AsNoTracking()
            join province in db.AdministrativeProvinces on room.Property.ProvinceCode equals province.Code
            join ward in db.AdministrativeWards
                on new { room.Property.ProvinceCode, Code = room.Property.WardCode }
                equals new { ward.ProvinceCode, ward.Code }
            where room.Id == roomId
            select new
            {
                room.Id, room.RoomNumber, room.Floor, room.AreaSqm, room.RoomType,
                room.MaxOccupants, room.Status, room.AvailableFrom,
                Property = new RoomListingPropertySnapshot(
                    room.Property.Id, room.Property.Name, room.Property.PropertyType,
                    province.Code, province.FullName, ward.Code, ward.FullName,
                    ward.DistrictName, room.Property.AddressDetail)
            }).FirstOrDefaultAsync(cancellationToken);
        if (detail is null) return null;

        var amenities = await (
            from link in db.RoomAmenities.AsNoTracking()
            join amenity in db.Amenities on link.AmenityId equals amenity.Id
            where link.RoomId == roomId
            orderby amenity.Id
            select new RoomListingAmenitySnapshot(amenity.Id, amenity.Code, amenity.Name))
            .ToArrayAsync(cancellationToken);
        var tariffs = await db.UtilityTariffs.AsNoTracking()
            .Where(tariff => tariff.PropertyId == detail.Property.Id && tariff.RoomId == null)
            .OrderBy(tariff => tariff.TariffType)
            .Select(tariff => new RoomListingTariffSnapshot(
                tariff.Id, tariff.TariffType, tariff.CalcMethod, tariff.UnitPrice))
            .ToArrayAsync(cancellationToken);

        return new RoomListingDetailSnapshot(
            detail.Id, detail.RoomNumber, detail.Floor, detail.AreaSqm, detail.RoomType,
            detail.MaxOccupants, detail.Status, detail.AvailableFrom, detail.Property, amenities, tariffs);
    }

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
                && (rate.TariffType == "ELECTRICITY" || rate.TariffType == "WATER"))
            .Select(rate => rate.TariffType).Distinct().ToListAsync(cancellationToken);
        return types.Count == 2;
    }
}
