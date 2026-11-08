using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Domain.Entities;

namespace HomeMatch.Modules.Properties.Application;

internal static class PropertyMapping
{
    public static PropertyResult ToResult(Property property) =>
        new(property.Id, property.Name, property.ProvinceCode, property.WardCode,
            property.AddressDetail, property.PropertyType, property.Latitude, property.Longitude);

    /// Đóng gói đối tượng RoomResult kèm danh sách ID tiện ích
    public static RoomResult ToResult(Room room, IReadOnlyList<long> amenityIds) =>
        new(room.Id, room.PropertyId, room.RoomNumber, room.Floor, room.AreaSqm,
            room.RoomType, room.MaxOccupants, room.BasePrice, room.Status, room.AvailableFrom, amenityIds);

    public static TariffResult ToResult(UtilityTariff tariff) =>
        new(tariff.Id, tariff.PropertyId, tariff.RoomId, tariff.TariffType,
            tariff.CalcMethod, tariff.UnitPrice);
}
