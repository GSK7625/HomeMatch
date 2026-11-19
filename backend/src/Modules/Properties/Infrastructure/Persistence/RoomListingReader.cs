using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class RoomListingReader(PropertiesDbContext db) : IRoomListingReader
{
    public async Task<IReadOnlyList<RoomListingSearchMatch>> SearchAvailableRoomsAsync(
        RoomListingSearchFilter filter, CancellationToken cancellationToken)
    {
        var roomsQuery = db.Rooms.AsNoTracking().Where(room => room.Status == "AVAILABLE");
        if (filter.ProvinceCode is not null)
            roomsQuery = roomsQuery.Where(room => room.Property.ProvinceCode == filter.ProvinceCode);
        if (filter.WardCode is not null)
            roomsQuery = roomsQuery.Where(room => room.Property.WardCode == filter.WardCode);
        if (filter.RoomType is not null)
            roomsQuery = roomsQuery.Where(room => room.RoomType == filter.RoomType);
        if (filter.Occupants.HasValue)
            roomsQuery = roomsQuery.Where(room => room.MaxOccupants >= filter.Occupants.Value);
        if (filter.MinArea.HasValue)
            roomsQuery = roomsQuery.Where(room => room.AreaSqm >= filter.MinArea.Value);
        if (filter.MaxArea.HasValue)
            roomsQuery = roomsQuery.Where(room => room.AreaSqm <= filter.MaxArea.Value);

        foreach (var amenityId in filter.AmenityIds.Distinct())
            roomsQuery = roomsQuery.Where(room => db.RoomAmenities.Any(link => link.RoomId == room.Id && link.AmenityId == amenityId));

        if (string.IsNullOrWhiteSpace(filter.Keyword))
        {
            return await roomsQuery.Select(room => new RoomListingSearchMatch(room.Id, true))
                .ToArrayAsync(cancellationToken);
        }

        var rawKw = filter.Keyword.Trim();
        var pattern = "%" + rawKw.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var normKw = VietnameseTextNormalizer.Normalize(rawKw);
        var normPattern = "%" + normKw.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

        var joined = from room in roomsQuery
                     join prop in db.Properties on room.PropertyId equals prop.Id
                     join province in db.AdministrativeProvinces on prop.ProvinceCode equals province.Code into provGroup
                     from province in provGroup.DefaultIfEmpty()
                     join ward in db.AdministrativeWards
                         on new { prop.ProvinceCode, Code = prop.WardCode }
                         equals new { ward.ProvinceCode, ward.Code } into wardGroup
                     from ward in wardGroup.DefaultIfEmpty()
                     select new
                     {
                         room.Id,
                         PropertyName = prop.Name,
                         Address = prop.AddressDetail,
                         District = ward != null ? ward.DistrictName : null,
                         Ward = ward != null ? ward.FullName : null,
                         WardShort = ward != null ? ward.Name : null,
                         Province = province != null ? province.FullName : null,
                         ProvinceShort = province != null ? province.Name : null
                     };

        return await joined.Select(item => new RoomListingSearchMatch(item.Id,
            EF.Functions.ILike(item.PropertyName, pattern, "\\")
            || EF.Functions.ILike(item.Address, pattern, "\\")
            || (item.District != null && EF.Functions.ILike(item.District, pattern, "\\"))
            || (item.Ward != null && EF.Functions.ILike(item.Ward, pattern, "\\"))
            || (item.WardShort != null && EF.Functions.ILike(item.WardShort, pattern, "\\"))
            || (item.Province != null && EF.Functions.ILike(item.Province, pattern, "\\"))
            || (item.ProvinceShort != null && EF.Functions.ILike(item.ProvinceShort, pattern, "\\"))
            || EF.Functions.ILike(EF.Functions.Unaccent(item.PropertyName), normPattern, "\\")
            || EF.Functions.ILike(EF.Functions.Unaccent(item.Address), normPattern, "\\")
            || (item.District != null && EF.Functions.ILike(EF.Functions.Unaccent(item.District), normPattern, "\\"))
            || (item.Ward != null && EF.Functions.ILike(EF.Functions.Unaccent(item.Ward), normPattern, "\\"))
            || (item.WardShort != null && EF.Functions.ILike(EF.Functions.Unaccent(item.WardShort), normPattern, "\\"))
            || (item.Province != null && EF.Functions.ILike(EF.Functions.Unaccent(item.Province), normPattern, "\\"))
            || (item.ProvinceShort != null && EF.Functions.ILike(EF.Functions.Unaccent(item.ProvinceShort), normPattern, "\\"))
        )).ToArrayAsync(cancellationToken);
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
