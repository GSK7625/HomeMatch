using HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListingById;
using HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.UnitTests;

public class GetPublicListingByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenTenantViewsPublishedListing_ShouldRecordViewAndIncrementViewCount()
    {
        var listing = CreatePublishedListing(id: 10, landlordId: 99, viewCount: 5);
        var repo = new FakeListingRepository(listing, recordViewResult: true);
        var handler = new GetPublicListingByIdHandler(repo, new FakeRoomListingReader("AVAILABLE"));

        var result = await handler.HandleAsync(new GetPublicListingByIdQuery(10, ViewerUserId: 7, IpAddress: "10.0.0.1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value.Listing.ViewCount);
        Assert.Single(repo.RecordedViews);
        Assert.Equal((10L, 7L, "10.0.0.1"), repo.RecordedViews[0]);
    }

    [Fact]
    public async Task HandleAsync_WhenViewIsDeduplicated_ShouldNotIncrementViewCount()
    {
        var listing = CreatePublishedListing(id: 10, landlordId: 99, viewCount: 5);
        var repo = new FakeListingRepository(listing, recordViewResult: false);
        var handler = new GetPublicListingByIdHandler(repo, new FakeRoomListingReader("AVAILABLE"));

        var result = await handler.HandleAsync(new GetPublicListingByIdQuery(10, ViewerUserId: 7, IpAddress: "10.0.0.1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Listing.ViewCount);
        Assert.Single(repo.RecordedViews);
    }

    [Fact]
    public async Task HandleAsync_WhenLandlordViewsOwnListing_ShouldSkipRecordingView()
    {
        var listing = CreatePublishedListing(id: 10, landlordId: 99, viewCount: 5);
        var repo = new FakeListingRepository(listing, recordViewResult: true);
        var handler = new GetPublicListingByIdHandler(repo, new FakeRoomListingReader("AVAILABLE"));

        var result = await handler.HandleAsync(new GetPublicListingByIdQuery(10, ViewerUserId: 99, IpAddress: "10.0.0.1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Listing.ViewCount);
        Assert.Empty(repo.RecordedViews);
    }

    [Fact]
    public async Task HandleAsync_WhenAnonymousVisitorViewsListing_ShouldRecordIpView()
    {
        var listing = CreatePublishedListing(id: 10, landlordId: 99, viewCount: 0);
        var repo = new FakeListingRepository(listing, recordViewResult: true);
        var handler = new GetPublicListingByIdHandler(repo, new FakeRoomListingReader("AVAILABLE"));

        var result = await handler.HandleAsync(new GetPublicListingByIdQuery(10, ViewerUserId: null, IpAddress: "203.0.113.5"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Listing.ViewCount);
        Assert.Single(repo.RecordedViews);
        Assert.Equal((10L, (long?)null, "203.0.113.5"), repo.RecordedViews[0]);
    }

    [Fact]
    public async Task HandleAsync_WhenListingNotPublished_ShouldReturnFailureWithoutRecordingView()
    {
        var listing = CreatePublishedListing(id: 10, landlordId: 99, viewCount: 5);
        listing.Status = "DRAFT";
        var repo = new FakeListingRepository(listing, recordViewResult: true);
        var handler = new GetPublicListingByIdHandler(repo, new FakeRoomListingReader("AVAILABLE"));

        var result = await handler.HandleAsync(new GetPublicListingByIdQuery(10, ViewerUserId: 7, IpAddress: "10.0.0.1"));

        Assert.True(result.IsFailure);
        Assert.Empty(repo.RecordedViews);
    }

    private static Listing CreatePublishedListing(long id, long landlordId, long viewCount) => new()
    {
        Id = id,
        RoomId = 20,
        LandlordId = landlordId,
        Title = "Phong tro test",
        Slug = "phong-tro-test",
        Description = "Mo ta",
        Price = 3_500_000,
        DepositAmount = 3_500_000,
        MinLeaseMonths = 6,
        Status = "PUBLISHED",
        ViewCount = viewCount,
        PublishedAt = DateTimeOffset.UtcNow.AddDays(-1),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
    };

    private sealed class FakeListingRepository(Listing? listing, bool recordViewResult) : IListingRepository
    {
        public List<(long ListingId, long? UserId, string? IpAddress)> RecordedViews { get; } = [];

        public Task<int> ExpirePublishedAsync(DateTimeOffset now, long? roomId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<(IReadOnlyList<Listing> Items, int TotalCount)> SearchPublicAsync(
            SearchListingsQuery query, long[] roomIds, long[] keywordRoomIds, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<Listing>, int)>(([], 0));

        public Task<Listing?> GetByIdAsync(long listingId, CancellationToken cancellationToken) =>
            Task.FromResult(listing?.Id == listingId ? listing : null);

        public Task<IReadOnlyList<Listing>> GetPublicByRoomIdsAsync(IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Listing>>([]);

        public Task<Listing?> GetOwnedAsync(long landlordId, long listingId, CancellationToken cancellationToken) =>
            Task.FromResult<Listing?>(null);

        public Task<IReadOnlyList<Listing>> GetMineAsync(long landlordId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Listing>>([]);

        public Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<Listing>, int)>(([], 0));

        public Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetAdminListingsAsync(
            string? status, string? keyword, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<Listing>, int)>(([], 0));

        public Task<AdminListingStatsSnapshot> GetAdminStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminListingStatsSnapshot(0, 0, 0, 0, 0, 0, 0, 0, 0));

        public Task<IReadOnlyList<Listing>> GetPublicCandidatesAsync(int offset, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Listing>>([]);

        public Task<bool> SlugExistsAsync(string slug, long? exceptId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> HasOtherPublishedAsync(long roomId, long listingId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> RoomHasListingAsync(long roomId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<ListingCoordinates?> GetCoordinatesAsync(long listingId, CancellationToken cancellationToken) =>
            Task.FromResult<ListingCoordinates?>(new ListingCoordinates(105.8, 21.0));

        public Task<IReadOnlyDictionary<long, ListingCoordinates>> GetCoordinatesBatchAsync(
            IReadOnlyCollection<long> listingIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<long, ListingCoordinates>>(new Dictionary<long, ListingCoordinates>());

        public Task SetCoordinatesAsync(Listing listing, double longitude, double latitude, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> RecordViewAsync(long listingId, long? userId, string? ipAddress, DateTimeOffset viewedAt, CancellationToken cancellationToken)
        {
            RecordedViews.Add((listingId, userId, ipAddress));
            return Task.FromResult(recordViewResult);
        }

        public void Add(Listing entity) { }

        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FakeRoomListingReader(string status) : IRoomListingReader
    {
        public Task<IReadOnlyList<RoomListingSearchMatch>> SearchAvailableRoomsAsync(
            RoomListingSearchFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoomListingSearchMatch>>([]);

        public Task<RoomListingSnapshot?> GetRoomAsync(long roomId, CancellationToken cancellationToken) =>
            Task.FromResult<RoomListingSnapshot?>(null);

        public Task<RoomListingDetailSnapshot?> GetRoomDetailAsync(long roomId, CancellationToken cancellationToken) =>
            Task.FromResult<RoomListingDetailSnapshot?>(new RoomListingDetailSnapshot(
                roomId,
                "101",
                1,
                25m,
                "STUDIO",
                2,
                status,
                DateOnly.FromDateTime(DateTime.UtcNow),
                new RoomListingPropertySnapshot(1, "Nha tro A", "BOARDING_HOUSE", "01", "Ha Noi", "001", "Dich Vong", "Cau Giay", "So 1"),
                [],
                []));

        public Task<IReadOnlyDictionary<long, RoomListingSnapshot>> GetRoomsAsync(
            IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<long, RoomListingSnapshot>>(new Dictionary<long, RoomListingSnapshot>());

        public Task<IReadOnlyDictionary<long, RoomCardSummary>> GetRoomCardSummariesAsync(
            IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<long, RoomCardSummary>>(new Dictionary<long, RoomCardSummary>());

        public Task<bool> HasElectricityAndWaterRatesAsync(long roomId, DateOnly date, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<RoomListingTariffSnapshot>> GetEffectiveRoomTariffsAsync(long roomId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoomListingTariffSnapshot>>([]);

        public Task<bool> RoomTypeExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
