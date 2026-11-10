using HomeMatch.Modules.Listings.Application.Models;
using HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IListingRepository
{
    Task<int> ExpirePublishedAsync(DateTimeOffset now, long? roomId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Listing> Items, int TotalCount)> SearchPublicAsync(
        SearchListingsQuery query, long[] roomIds, long[] keywordRoomIds, CancellationToken cancellationToken);
    Task<Listing?> GetByIdAsync(long listingId, CancellationToken cancellationToken);
    Task<Listing?> GetOwnedAsync(long landlordId, long listingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Listing>> GetMineAsync(long landlordId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Listing>> GetPublicCandidatesAsync(int offset, int limit, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, long? exceptId, CancellationToken cancellationToken);
    Task<bool> HasOtherPublishedAsync(long roomId, long listingId, CancellationToken cancellationToken);
    Task<ListingCoordinates?> GetCoordinatesAsync(long listingId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<long, ListingCoordinates>> GetCoordinatesBatchAsync(IReadOnlyCollection<long> listingIds, CancellationToken cancellationToken);
    Task SetCoordinatesAsync(Listing listing, double longitude, double latitude, CancellationToken cancellationToken);
    void Add(Listing listing);
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
