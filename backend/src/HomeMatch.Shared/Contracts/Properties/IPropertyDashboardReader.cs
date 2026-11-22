namespace HomeMatch.Shared.Contracts.Properties;

public sealed record DashboardRoomInfo(
    long RoomId,
    long PropertyId,
    string PropertyName,
    string RoomNumber,
    string Status);

public interface IPropertyDashboardReader
{
    Task<IReadOnlyList<DashboardRoomInfo>> GetRoomsForDashboardAsync(
        long? landlordId,
        long? propertyId = null,
        CancellationToken cancellationToken = default);
}
