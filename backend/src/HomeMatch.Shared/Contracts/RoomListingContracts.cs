namespace HomeMatch.Shared.Contracts;

public sealed record RoomListingSnapshot(
    long RoomId,
    long PropertyId,
    long LandlordId,
    string Status,
    string ProvinceCode,
    string WardCode,
    string AddressDetail);

public interface IRoomListingReader
{
    Task<RoomListingSnapshot?> GetRoomAsync(long roomId, CancellationToken cancellationToken);
    Task<RoomListingDetailSnapshot?> GetRoomDetailAsync(long roomId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<long, RoomListingSnapshot>> GetRoomsAsync(
        IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken);
    Task<bool> HasElectricityAndWaterRatesAsync(
        long roomId, DateOnly date, CancellationToken cancellationToken);
}

public sealed record RoomListingDetailSnapshot(
    long RoomId,
    string RoomNumber,
    int Floor,
    decimal AreaSqm,
    string RoomType,
    int MaxOccupants,
    string Status,
    DateOnly AvailableFrom,
    RoomListingPropertySnapshot Property,
    IReadOnlyList<RoomListingAmenitySnapshot> Amenities,
    IReadOnlyList<RoomListingTariffSnapshot> Tariffs);

public sealed record RoomListingPropertySnapshot(
    long Id,
    string Name,
    string PropertyType,
    string ProvinceCode,
    string ProvinceName,
    string WardCode,
    string WardName,
    string? DistrictName,
    string AddressDetail);

public sealed record RoomListingAmenitySnapshot(long Id, string Code, string Name);

public sealed record RoomListingTariffSnapshot(long Id, string TariffType, string CalcMethod, long UnitPrice);

public interface IListingVisibilityWriter
{
    Task HidePublishedForRoomAsync(long roomId, CancellationToken cancellationToken);
}
