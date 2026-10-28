namespace HomeMatch.Shared.Contracts;

public class DefaultRoomOwnershipService : IRoomOwnershipService
{
    public virtual Task<bool> IsLandlordOwnerOfRoomAsync(long landlordUserId, long roomId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(landlordUserId > 0 && roomId > 0);
    }

    public virtual Task<bool> IsRoomAvailableForLeaseAsync(long roomId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public virtual Task SetRoomStatusAsync(long roomId, string status, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
