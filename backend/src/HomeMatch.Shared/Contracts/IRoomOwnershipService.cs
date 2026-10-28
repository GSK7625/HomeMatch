namespace HomeMatch.Shared.Contracts;

public interface IRoomOwnershipService
{
    Task<bool> IsLandlordOwnerOfRoomAsync(long landlordUserId, long roomId, CancellationToken cancellationToken = default);
    Task<bool> IsRoomAvailableForLeaseAsync(long roomId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task SetRoomStatusAsync(long roomId, string status, CancellationToken cancellationToken = default);
}
