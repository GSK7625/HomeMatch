namespace HomeMatch.Shared.Contracts.Listings;

public interface IListingContractReader
{
    Task<ListingContractInfo?> GetListingByIdAsync(long listingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, ListingContractInfo>> GetListingsByIdsAsync(IEnumerable<long> listingIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetListingIdsByLandlordIdAsync(long landlordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetListingIdsByRoomIdAsync(long roomId, CancellationToken cancellationToken = default);
}
