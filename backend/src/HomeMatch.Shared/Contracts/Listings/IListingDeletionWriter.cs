using HomeMatch.Shared.Common;

namespace HomeMatch.Shared.Contracts.Listings;

public interface IListingDeletionWriter
{
    Task<Result> DeleteForRoomAsync(long landlordId, long roomId, DateTimeOffset deletedAt, CancellationToken cancellationToken);
}
