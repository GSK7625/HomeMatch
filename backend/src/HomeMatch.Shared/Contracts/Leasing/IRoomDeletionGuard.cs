namespace HomeMatch.Shared.Contracts.Leasing;

public interface IRoomDeletionGuard
{
    Task<bool> HasReservingContractsAsync(long roomId, CancellationToken cancellationToken);
}
