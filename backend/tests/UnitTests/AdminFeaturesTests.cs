using HomeMatch.Modules.Identity.Application.Features.Admin.SetUserLockStatus;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Listings.Application.Features.AdminDashboard;
using HomeMatch.Modules.Listings.Application.Features.Moderation.AdminHideListing;
using HomeMatch.Modules.Listings.Application.Features.Moderation.GetAdminListings;
using HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;
using HomeMatch.Modules.Listings.Application.Features.Reports;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Application.DTOs;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Features.AdminCatalogs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Admin;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.UnitTests;

public class AdminFeaturesTests
{
    [Fact]
    public async Task SetUserLockStatus_WhenLockingSelf_ShouldFail()
    {
        var handler = new SetUserLockStatusHandler(new FakeAdminUserService());
        var result = await handler.HandleAsync(new SetUserLockStatusCommand(AdminId: 1, TargetUserId: 1, Lock: true));
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.CannotLockSelf", result.Error.Code);
    }

    [Fact]
    public async Task SetUserLockStatus_WhenLockingAndUnlockingTenant_ShouldSucceed()
    {
        var svc = new FakeAdminUserService();
        var handler = new SetUserLockStatusHandler(svc);

        var lockRes = await handler.HandleAsync(new SetUserLockStatusCommand(AdminId: 1, TargetUserId: 3, Lock: true));
        Assert.True(lockRes.IsSuccess);
        Assert.Equal("Suspended", lockRes.Value.Status);

        var unlockRes = await handler.HandleAsync(new SetUserLockStatusCommand(AdminId: 1, TargetUserId: 3, Lock: false));
        Assert.True(unlockRes.IsSuccess);
        Assert.Equal("Active", unlockRes.Value.Status);
    }

    [Fact]
    public async Task AdminHideListing_WhenPublished_ShouldSetHiddenAndReason()
    {
        var listing = new Listing { Id = 12, LandlordId = 2, RoomId = 5, Title = "Tin vi pham", Status = "PUBLISHED" };
        var repo = new FakeListingsRepo(listing);
        var handler = new AdminHideListingHandler(repo);

        var res = await handler.HandleAsync(new AdminHideListingCommand(1, 12, "Gia mao dia chi"));

        Assert.True(res.IsSuccess);
        Assert.Equal("HIDDEN", res.Value.Status);
        Assert.Equal("Gia mao dia chi", listing.RejectionReason);
    }

    [Fact]
    public async Task ViolationReport_CreateAndResolveWithHideListing_ShouldHideListing()
    {
        var listing = new Listing { Id = 15, LandlordId = 2, RoomId = 5, Title = "Phong tro A", Status = "PUBLISHED" };
        var listingsRepo = new FakeListingsRepo(listing);
        var reportsRepo = new FakeViolationReportRepo();
        var users = new FakeIdentityReader();

        var createHandler = new CreateViolationReportHandler(reportsRepo, listingsRepo, users);
        var created = await createHandler.HandleAsync(new CreateViolationReportCommand(
            ReporterUserId: 3,
            TargetType: "LISTING",
            TargetId: 15,
            ListingId: 15,
            Reason: "Tin dang lua dao",
            Description: "Yeu cau coc truoc khi xem",
            EvidenceUrl: "https://example.com/proof.png"));

        Assert.True(created.IsSuccess);
        Assert.Equal("PENDING", created.Value.Status);

        var resolveHandler = new ResolveViolationReportHandler(reportsRepo, listingsRepo, users);
        var resolved = await resolveHandler.HandleAsync(new ResolveViolationReportCommand(
            AdminId: 1,
            ReportId: created.Value.Id,
            Resolution: "RESOLVED",
            AdminNote: "Da xac minh vi pham",
            HideListing: true));

        Assert.True(resolved.IsSuccess);
        Assert.Equal("RESOLVED", resolved.Value.Status);
        Assert.Equal("HIDDEN", listing.Status);

        var secondResolve = await resolveHandler.HandleAsync(new ResolveViolationReportCommand(1, created.Value.Id, "DISMISSED", null, false));
        Assert.True(secondResolve.IsFailure);
        Assert.Equal("Report.AlreadyResolved", secondResolve.Error.Code);
    }

    [Fact]
    public async Task AdminCatalog_CreateRoomTypeAndToggleAmenity_ShouldWork()
    {
        var repo = new FakeCatalogRepo();
        var createRoomType = new CreateRoomTypeHandler(repo);
        var rtRes = await createRoomType.HandleAsync(new CreateRoomTypeCommand("loft_studio", "Phong gac lung", true));
        Assert.True(rtRes.IsSuccess);
        Assert.Equal("LOFT_STUDIO", rtRes.Value.Code);

        var dupRes = await createRoomType.HandleAsync(new CreateRoomTypeCommand("LOFT_STUDIO", "Ten khac", true));
        Assert.True(dupRes.IsFailure);
        Assert.Equal("Catalog.Duplicate", dupRes.Error.Code);

        var updateAmenity = new UpdateAmenityHandler(repo);
        var amRes = await updateAmenity.HandleAsync(new UpdateAmenityCommand(1, null, "Dieu hoa Inverter", false));
        Assert.True(amRes.IsSuccess);
        Assert.False(amRes.Value.IsActive);
        Assert.Equal("Dieu hoa Inverter", amRes.Value.Name);
    }

    [Fact]
    public async Task GetAdminDashboard_WithMonthPeriod_ShouldAggregateAllModules()
    {
        var listing = new Listing { Id = 1, LandlordId = 2, RoomId = 1, Title = "T1", Status = "PUBLISHED", ViewCount = 12, CreatedAt = DateTimeOffset.UtcNow };
        var handler = new GetAdminDashboardHandler(
            new FakeIdentityStatsReader(),
            new FakePropertyStatsReader(),
            new FakeListingsRepo(listing),
            new FakeViolationReportRepo());

        var res = await handler.HandleAsync(new GetAdminDashboardQuery("month"));

        Assert.True(res.IsSuccess);
        Assert.Equal("month", res.Value.Period);
        Assert.NotNull(res.Value.FromUtc);
        Assert.Equal(10, res.Value.Users.TotalUsers);
        Assert.Equal(5, res.Value.Properties.TotalRooms);
        Assert.Equal(1, res.Value.Listings.PublishedListings);
    }

    private sealed class FakeAdminUserService : IAdminUserService
    {
        public Task<PagedAdminUsersResult> GetUsersAsync(string? search, string? role, string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedAdminUsersResult([], page, pageSize, 0));

        public Task<Result<AdminUserItemDto>> SetUserStatusAsync(long targetUserId, UserStatus newStatus, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new AdminUserItemDto(targetUserId, "Tenant", "t@test.vn", null, "Tenant", newStatus.ToString(), true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
    }

    private sealed class FakeListingsRepo(Listing listing) : IListingRepository
    {
        public Task<int> ExpirePublishedAsync(DateTimeOffset now, long? roomId, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<(IReadOnlyList<ListingSearchItemDto> Items, int TotalCount)> SearchPublicAsync(SearchListingsQuery query, long[] roomIds, long[] keywordRoomIds, CancellationToken cancellationToken) => Task.FromResult<(IReadOnlyList<ListingSearchItemDto>, int)>(([], 0));
        public Task<Listing?> GetByIdAsync(long listingId, CancellationToken cancellationToken) => Task.FromResult(listing.Id == listingId ? listing : null);
        public Task<IReadOnlyList<Listing>> GetPublicByRoomIdsAsync(IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Listing>>([]);
        public Task<Listing?> GetOwnedAsync(long landlordId, long listingId, CancellationToken cancellationToken) => Task.FromResult<Listing?>(null);
        public Task<Listing?> GetOwnedForDeletionAsync(long landlordId, long listingId, CancellationToken cancellationToken) => Task.FromResult<Listing?>(null);
        public Task<IReadOnlyList<Listing>> GetMineAsync(long landlordId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Listing>>([]);
        public Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<(IReadOnlyList<Listing>, int)>(([], 0));
        public Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetAdminListingsAsync(string? status, string? keyword, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<(IReadOnlyList<Listing>, int)>(([listing], 1));
        public Task<AdminListingStatsSnapshot> GetAdminStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken) => Task.FromResult(new AdminListingStatsSnapshot(1, 1, 0, 0, 0, 0, 0, listing.ViewCount, 1));
        public Task<IReadOnlyList<Listing>> GetPublicCandidatesAsync(int offset, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Listing>>([]);
        public Task<bool> SlugExistsAsync(string slug, long? exceptId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasOtherPublishedAsync(long roomId, long listingId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> RoomHasListingAsync(long roomId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<ListingCoordinates?> GetCoordinatesAsync(long listingId, CancellationToken cancellationToken) => Task.FromResult<ListingCoordinates?>(null);
        public Task<IReadOnlyDictionary<long, ListingCoordinates>> GetCoordinatesBatchAsync(IReadOnlyCollection<long> listingIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<long, ListingCoordinates>>(new Dictionary<long, ListingCoordinates>());
        public Task SetCoordinatesAsync(Listing l, double longitude, double latitude, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> RecordViewAsync(long listingId, long? userId, string? ipAddress, DateTimeOffset viewedAt, CancellationToken cancellationToken) => Task.FromResult(false);
        public void Add(Listing l) { }
        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    private sealed class FakeViolationReportRepo : IViolationReportRepository
    {
        private readonly List<ViolationReport> _items = [];
        private long _nextId = 1;

        public void Add(ViolationReport report)
        {
            report.Id = _nextId++;
            _items.Add(report);
        }

        public Task<ViolationReport?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

        public Task<(IReadOnlyList<ViolationReport> Items, int TotalCount)> GetPagedAsync(string? status, string? targetType, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<ViolationReport>, int)>((_items, _items.Count));

        public Task<AdminReportStatsSnapshot> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminReportStatsSnapshot(_items.Count, _items.Count(r => r.Status == "PENDING"), _items.Count(r => r.Status == "RESOLVED"), _items.Count(r => r.Status == "DISMISSED"), _items.Count));

        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    private sealed class FakeIdentityReader : IIdentityUserReader
    {
        public Task<IdentityUserInfo?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IdentityUserInfo?>(new IdentityUserInfo(userId, $"User {userId}", $"user{userId}@test.vn", null, "Tenant"));

        public Task<IReadOnlyList<IdentityUserInfo>> GetTenantsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IdentityUserInfo>>([]);
    }

    private sealed class FakeCatalogRepo : IAdminCatalogRepository
    {
        private readonly List<Amenity> _amenities = [new() { Id = 1, Code = "AIR_CONDITIONER", Name = "Dieu hoa", IsActive = true }];
        private readonly List<RoomTypeCatalog> _roomTypes = [];
        private long _nextId = 1;

        public Task<IReadOnlyList<AmenityResult>> GetAllAmenitiesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AmenityResult>>(_amenities.Select(a => new AmenityResult(a.Id, a.Code, a.Name, a.IsActive)).ToList());
        public Task<Amenity?> GetAmenityByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_amenities.FirstOrDefault(a => a.Id == id));
        public Task<bool> AmenityCodeExistsAsync(string code, long? exceptId, CancellationToken cancellationToken) =>
            Task.FromResult(_amenities.Any(a => a.Code == code && (!exceptId.HasValue || a.Id != exceptId.Value)));
        public Task<bool> AmenityNameExistsAsync(string name, long? exceptId, CancellationToken cancellationToken) =>
            Task.FromResult(_amenities.Any(a => a.Name == name && (!exceptId.HasValue || a.Id != exceptId.Value)));
        public void AddAmenity(Amenity amenity) { amenity.Id = ++_nextId; _amenities.Add(amenity); }
        public Task<IReadOnlyList<RoomTypeResult>> GetAllRoomTypesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoomTypeResult>>(_roomTypes.Select(r => new RoomTypeResult(r.Id, r.Code, r.Name, r.IsActive)).ToList());
        public Task<RoomTypeCatalog?> GetRoomTypeByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_roomTypes.FirstOrDefault(r => r.Id == id));
        public Task<bool> RoomTypeCodeExistsAsync(string code, long? exceptId, CancellationToken cancellationToken) =>
            Task.FromResult(_roomTypes.Any(r => r.Code == code && (!exceptId.HasValue || r.Id != exceptId.Value)));
        public Task<bool> RoomTypeNameExistsAsync(string name, long? exceptId, CancellationToken cancellationToken) =>
            Task.FromResult(_roomTypes.Any(r => r.Name == name && (!exceptId.HasValue || r.Id != exceptId.Value)));
        public void AddRoomType(RoomTypeCatalog roomType) { roomType.Id = _nextId++; _roomTypes.Add(roomType); }
        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    private sealed class FakeIdentityStatsReader : IAdminIdentityStatsReader
    {
        public Task<AdminIdentityStats> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdminIdentityStats(10, 7, 2, 1, 4));
    }

    private sealed class FakePropertyStatsReader : IAdminPropertyStatsReader
    {
        public Task<AdminPropertyStats> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdminPropertyStats(2, 5, 3, 2, 0, 2));
    }
}
