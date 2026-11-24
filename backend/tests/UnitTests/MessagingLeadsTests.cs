using HomeMatch.Modules.Messaging.Application.Commands.UpdateLeadStatus;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Application.Queries.GetLandlordLeads;
using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Properties;

namespace HomeMatch.UnitTests;

public class MessagingLeadsTests
{
    [Fact]
    public async Task GetLandlordLeads_ShouldFilterByListingRoomStatusAndSearch()
    {
        var leads = new List<Lead>
        {
            new()
            {
                Id = 1,
                TenantId = 101,
                ListingId = 10,
                Status = LeadStatus.Contacted,
                Source = LeadSource.Direct,
                Notes = "Hỏi xem phòng cuối tuần",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = 2,
                TenantId = 102,
                ListingId = 20,
                Status = LeadStatus.ViewingScheduled,
                Source = LeadSource.SearchFilter,
                Notes = "Đã hẹn xem tối thứ 6",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
            }
        };

        var handler = new GetLandlordLeadsHandler(
            new FakeLeadRepository(leads, new Dictionary<(long, long), long> { [(101, 10)] = 500 }),
            new FakeListingDashboardReader(
            [
                new DashboardListingInfo(10, 1001, 2, "Phòng studio Giải Phóng", "PUBLISHED", 120),
                new DashboardListingInfo(20, 1002, 2, "Phòng 1PN Trịnh Đình Cửu", "PUBLISHED", 80)
            ]),
            new FakePropertyDashboardReader(
            [
                new DashboardRoomInfo(1001, 50, "Nhà trọ Giải Phóng", "101", "AVAILABLE"),
                new DashboardRoomInfo(1002, 51, "Chung cư mini Định Công", "502", "AVAILABLE")
            ]),
            new FakeIdentityUserReader(
            [
                new IdentityUserInfo(101, "Trần Bình An", "an@homematch.vn", "0903000001", "Tenant"),
                new IdentityUserInfo(102, "Nguyễn Thu Hà", "ha@homematch.vn", "0903000002", "Tenant")
            ]));

        var allResult = await handler.HandleAsync(new GetLandlordLeadsQuery(LandlordId: 2));
        Assert.True(allResult.IsSuccess);
        Assert.Equal(2, allResult.Value.TotalCount);
        Assert.Equal(1, allResult.Value.Summary.ContactedCount);
        Assert.Equal(1, allResult.Value.Summary.ViewingScheduledCount);
        Assert.Equal(2, allResult.Value.ListingOptions.Count);
        Assert.Equal(500, allResult.Value.Items.First(i => i.Id == 1).ConversationId);

        var filtered = await handler.HandleAsync(new GetLandlordLeadsQuery(
            LandlordId: 2,
            RoomId: 1002,
            Status: LeadStatus.ViewingScheduled,
            Search: "Thu Hà"));
        Assert.True(filtered.IsSuccess);
        Assert.Single(filtered.Value.Items);
        Assert.Equal(2, filtered.Value.Items[0].Id);
        Assert.Equal("502", filtered.Value.Items[0].RoomNumber);
    }

    [Fact]
    public async Task GetLandlordLeads_WhenStatusInvalid_ShouldFail()
    {
        var handler = new GetLandlordLeadsHandler(
            new FakeLeadRepository([], new Dictionary<(long, long), long>()),
            new FakeListingDashboardReader([]),
            new FakePropertyDashboardReader([]),
            new FakeIdentityUserReader([]));

        var result = await handler.HandleAsync(new GetLandlordLeadsQuery(LandlordId: 2, Status: "UNKNOWN_STATUS"));
        Assert.True(result.IsFailure);
        Assert.Equal("Messaging.InvalidLeadStatus", result.Error.Code);
    }

    [Fact]
    public async Task UpdateLeadStatus_WhenOwnedByLandlord_ShouldUpdateStatusAndNotes()
    {
        var lead = new Lead
        {
            Id = 7,
            TenantId = 101,
            ListingId = 10,
            Status = LeadStatus.Contacted,
            Source = LeadSource.Direct
        };
        var repo = new FakeLeadRepository([lead], new Dictionary<(long, long), long> { [(101, 10)] = 999 });
        var handler = new UpdateLeadStatusHandler(
            repo,
            new FakeListingDashboardReader([new DashboardListingInfo(10, 1001, 2, "Phòng 101", "PUBLISHED", 10)]),
            new FakePropertyDashboardReader([new DashboardRoomInfo(1001, 50, "Nhà A", "101", "AVAILABLE")]),
            new FakeIdentityUserReader([new IdentityUserInfo(101, "Trần Bình An", "an@homematch.vn", "0903000001", "Tenant")]));

        var res = await handler.HandleAsync(new UpdateLeadStatusCommand(
            LeadId: 7,
            LandlordId: 2,
            Status: LeadStatus.ViewingScheduled,
            Notes: "  Hẹn xem lúc 18h  ",
            UpdateNotes: true));

        Assert.True(res.IsSuccess);
        Assert.Equal(LeadStatus.ViewingScheduled, res.Value.Status);
        Assert.Equal("Hẹn xem lúc 18h", res.Value.Notes);
        Assert.Equal(999, res.Value.ConversationId);
        Assert.True(repo.Saved);
    }

    [Fact]
    public async Task UpdateLeadStatus_WhenLeadBelongsToAnotherLandlord_ShouldReturnForbidden()
    {
        var lead = new Lead
        {
            Id = 8,
            TenantId = 101,
            ListingId = 99,
            Status = LeadStatus.New
        };
        var handler = new UpdateLeadStatusHandler(
            new FakeLeadRepository([lead], new Dictionary<(long, long), long>()),
            new FakeListingDashboardReader([new DashboardListingInfo(10, 1001, 2, "Tin của chủ nhà 2", "PUBLISHED", 10)]),
            new FakePropertyDashboardReader([]),
            new FakeIdentityUserReader([]));

        var res = await handler.HandleAsync(new UpdateLeadStatusCommand(
            LeadId: 8,
            LandlordId: 2,
            Status: LeadStatus.Contacted,
            Notes: null,
            UpdateNotes: false));

        Assert.True(res.IsFailure);
        Assert.Equal("Messaging.LeadForbidden", res.Error.Code);
    }

    private sealed class FakeLeadRepository(
        List<Lead> leads,
        Dictionary<(long TenantId, long ListingId), long> conversationMap) : ILeadRepository
    {
        public bool Saved { get; private set; }

        public Task<IReadOnlyList<Lead>> GetByListingIdsAsync(
            IReadOnlyCollection<long> listingIds,
            CancellationToken cancellationToken = default)
        {
            var result = leads.Where(l => listingIds.Contains(l.ListingId)).ToList();
            return Task.FromResult<IReadOnlyList<Lead>>(result);
        }

        public Task<Lead?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(leads.FirstOrDefault(l => l.Id == id));
        }

        public Task<Dictionary<(long TenantId, long ListingId), long>> GetConversationMapByListingIdsAsync(
            IReadOnlyCollection<long> listingIds,
            long landlordId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(conversationMap);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeListingDashboardReader(IReadOnlyList<DashboardListingInfo> items) : IListingDashboardReader
    {
        public Task<IReadOnlyList<DashboardListingInfo>> GetListingsForDashboardAsync(
            long? landlordId,
            IReadOnlyCollection<long>? roomIds = null,
            CancellationToken cancellationToken = default)
        {
            var filtered = items.Where(l => !landlordId.HasValue || l.LandlordId == landlordId.Value).ToList();
            return Task.FromResult<IReadOnlyList<DashboardListingInfo>>(filtered);
        }
    }

    private sealed class FakePropertyDashboardReader(IReadOnlyList<DashboardRoomInfo> items) : IPropertyDashboardReader
    {
        public Task<IReadOnlyList<DashboardRoomInfo>> GetRoomsForDashboardAsync(
            long? landlordId,
            long? propertyId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakeIdentityUserReader(IReadOnlyList<IdentityUserInfo> users) : IIdentityUserReader
    {
        public Task<IdentityUserInfo?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(users.FirstOrDefault(u => u.Id == userId));
        }

        public Task<IReadOnlyList<IdentityUserInfo>> GetTenantsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(users);
        }
    }
}
