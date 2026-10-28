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
    Task<IReadOnlyDictionary<long, RoomListingSnapshot>> GetRoomsAsync(
        IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken);
    Task<bool> HasElectricityAndWaterRatesAsync(
        long roomId, DateOnly date, CancellationToken cancellationToken);
}

public interface IListingVisibilityWriter
{
    Task HidePublishedForRoomAsync(long roomId, CancellationToken cancellationToken);
}
