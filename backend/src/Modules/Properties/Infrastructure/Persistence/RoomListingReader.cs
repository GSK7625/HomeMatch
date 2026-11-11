using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class RoomListingReader(PropertiesDbContext db) : IRoomListingReader
{
    public async Task<IReadOnlyList<RoomListingSearchMatch>> SearchAvailableRoomsAsync(
        RoomListingSearchFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Rooms.AsNoTracking().Where(room => room.Status == "AVAILABLE");
        if (filter.ProvinceCode is not null)
            query = query.Where(room => room.Property.ProvinceCode == filter.ProvinceCode);
        if (filter.WardCode is not null)
            query = query.Where(room => room.Property.WardCode == filter.WardCode);
        if (filter.RoomType is not null)
            query = query.Where(room => room.RoomType == filter.RoomType);
        if (filter.Occupants.HasValue)
            query = query.Where(room => room.MaxOccupants >= filter.Occupants.Value);
        if (filter.MinArea.HasValue)
            query = query.Where(room => room.AreaSqm >= filter.MinArea.Value);
        if (filter.MaxArea.HasValue)
            query = query.Where(room => room.AreaSqm <= filter.MaxArea.Value);

        foreach (var amenityId in filter.AmenityIds.Distinct())
            query = query.Where(room => db.RoomAmenities.Any(link => link.RoomId == room.Id && link.AmenityId == amenityId));

        var pattern = "%" + filter.Keyword?.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        return await query.Select(room => new RoomListingSearchMatch(room.Id,
            filter.Keyword == null
            || EF.Functions.ILike(room.Property.Name, pattern, "\\")
            || EF.Functions.ILike(room.Property.AddressDetail, pattern, "\\")))
            .ToArrayAsync(cancellationToken);
    }

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
                room.Property.ProvinceCode, room.Property.WardCode, room.Property.AddressDetail,
                room.Property.Latitude, room.Property.Longitude))
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
                room.Property.ProvinceCode, room.Property.WardCode, room.Property.AddressDetail,
                room.Property.Latitude, room.Property.Longitude))
            .ToListAsync(cancellationToken);
        return rooms.ToDictionary(room => room.RoomId);
    }

    public async Task<IReadOnlyDictionary<long, RoomCardSummary>> GetRoomCardSummariesAsync(
        IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken)
    {
        if (roomIds.Count == 0)
            return new Dictionary<long, RoomCardSummary>();

        var baseData = await (
            from room in db.Rooms.AsNoTracking()
            join province in db.AdministrativeProvinces on room.Property.ProvinceCode equals province.Code
            join ward in db.AdministrativeWards
                on new { room.Property.ProvinceCode, Code = room.Property.WardCode }
                equals new { ward.ProvinceCode, ward.Code }
            where roomIds.Contains(room.Id)
            select new
            {
                room.Id,
                room.PropertyId,
                room.RoomType,
                room.AreaSqm,
                ward.DistrictName,
                ProvinceName = province.FullName,
                room.Property.AddressDetail
            }).ToListAsync(cancellationToken);

        if (baseData.Count == 0)
            return new Dictionary<long, RoomCardSummary>();

        var propertyIds = baseData.Select(x => x.PropertyId).Distinct().ToList();

        var tariffs = await db.UtilityTariffs.AsNoTracking()
            .Where(t => propertyIds.Contains(t.PropertyId) && t.RoomId == null && (t.TariffType == "ELECTRICITY" || t.TariffType == "WATER"))
            .Select(t => new { t.PropertyId, t.TariffType, t.UnitPrice })
            .ToListAsync(cancellationToken);

        var tariffsByProperty = tariffs
            .GroupBy(t => t.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Electricity = g.FirstOrDefault(t => t.TariffType == "ELECTRICITY")?.UnitPrice,
                    Water = g.FirstOrDefault(t => t.TariffType == "WATER")?.UnitPrice
                });

        var amenities = await (
            from link in db.RoomAmenities.AsNoTracking()
            join amenity in db.Amenities on link.AmenityId equals amenity.Id
            where roomIds.Contains(link.RoomId)
            select new { link.RoomId, amenity.Name })
            .ToListAsync(cancellationToken);

        var amenitiesByRoom = amenities
            .GroupBy(a => a.RoomId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.Name).ToList());

        var result = new Dictionary<long, RoomCardSummary>();
        foreach (var b in baseData)
        {
            tariffsByProperty.TryGetValue(b.PropertyId, out var tariff);
            amenitiesByRoom.TryGetValue(b.Id, out var roomAmenities);

            result[b.Id] = new RoomCardSummary(
                b.Id,
                b.RoomType,
                b.AreaSqm,
                b.DistrictName,
                b.ProvinceName,
                b.AddressDetail,
                tariff?.Electricity,
                tariff?.Water,
                roomAmenities ?? []);
        }

        return result;
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
