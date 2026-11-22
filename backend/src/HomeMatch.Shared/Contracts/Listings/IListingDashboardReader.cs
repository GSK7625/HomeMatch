namespace HomeMatch.Shared.Contracts.Listings;

public sealed record DashboardListingInfo(
    long ListingId,
    long RoomId,
    long LandlordId,
    string Title,
    string Status,
    long ViewCount);

public interface IListingDashboardReader
{
    Task<IReadOnlyList<DashboardListingInfo>> GetListingsForDashboardAsync(
        long? landlordId,
        IReadOnlyCollection<long>? roomIds = null,
        CancellationToken cancellationToken = default);
}
