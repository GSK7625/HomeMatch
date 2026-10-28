using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Domain.Entities;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using HomeMatch.Modules.Leasing.Infrastructure.Services;
using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HomeMatch.UnitTests;

public class MockRoomOwnershipService : IRoomOwnershipService
{
    private readonly HashSet<(long landlordId, long roomId)> _ownedRooms = new();
    public string LastRoomStatusSet { get; private set; } = string.Empty;

    public void RegisterOwnership(long landlordId, long roomId)
    {
        _ownedRooms.Add((landlordId, roomId));
    }

    public Task<bool> IsLandlordOwnerOfRoomAsync(long landlordUserId, long roomId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_ownedRooms.Contains((landlordUserId, roomId)));
    }

    public Task<bool> IsRoomAvailableForLeaseAsync(long roomId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task SetRoomStatusAsync(long roomId, string status, CancellationToken cancellationToken = default)
    {
        LastRoomStatusSet = status;
        return Task.CompletedTask;
    }
}

public class LeasingServiceTests
{
    private LeasingDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LeasingDbContext>()
            .UseInMemoryDatabase(databaseName: $"LeasingTestDb_{Guid.NewGuid()}")
            .Options;

        return new LeasingDbContext(options);
    }

    #region 1 & 2: Quản lý khách quan tâm (UC3.7.1)

    [Fact]
    public async Task GetLandlordLeadsAsync_ReturnsMatchingLeads()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        var service = new LeasingService(context, mockRoomService);

        context.Leads.AddRange(
            new Lead { Id = 1, TenantId = 101, ListingId = 501, Status = LeadStatus.NEW, Notes = "Khách hỏi phòng" },
            new Lead { Id = 2, TenantId = 102, ListingId = 501, Status = LeadStatus.CONTACTED },
            new Lead { Id = 3, TenantId = 103, ListingId = 502, Status = LeadStatus.CLOSED }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetLandlordLeadsAsync(landlordUserId: 1, listingId: 501);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task UpdateLeadStatusAsync_ValidTransition_UpdatesStatusAndAppendsNote()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        var service = new LeasingService(context, mockRoomService);

        var lead = new Lead { Id = 1, TenantId = 101, ListingId = 501, Status = LeadStatus.NEW, Notes = "Ghi chú 1" };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLeadStatusAsync(
            landlordUserId: 1, 
            leadId: 1, 
            new UpdateLeadRequest(LeadStatus.CONTACTED, "Đã gọi điện thoại"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(LeadStatus.CONTACTED, result.Value.Status);
        Assert.Contains("Đã gọi điện thoại", result.Value.Notes);
    }

    [Fact]
    public async Task UpdateLeadStatusAsync_ClosedLead_ReopeningIsRejected()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        var service = new LeasingService(context, mockRoomService);

        var lead = new Lead { Id = 1, TenantId = 101, ListingId = 501, Status = LeadStatus.CLOSED };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLeadStatusAsync(
            landlordUserId: 1, 
            leadId: 1, 
            new UpdateLeadRequest(LeadStatus.CONTACTED, "Thử mở lại"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Lead.InvalidTransition", result.Error.Code);
    }

    #endregion

    #region 3 & 4: Quản lý danh sách người thuê (UC3.7.2)

    [Fact]
    public async Task GetLandlordTenantsAsync_OnlyIncludesActualLeaseContracts_NotLeadsOrDrafts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        var service = new LeasingService(context, mockRoomService);

        const long landlordId = 1;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Hợp đồng hợp lệ
        context.Contracts.Add(new Contract
        {
            Id = 1,
            ContractCode = "CTR-001",
            RoomId = 10,
            TenantId = 201,
            LandlordId = landlordId,
            StartDate = today.AddDays(-10),
            EndDate = today.AddMonths(6),
            MonthlyRent = 5000000,
            DepositAmount = 5000000,
            Status = ContractStatus.ACTIVE
        });

        // Bản nháp DRAFT (chưa phải người thuê chính thức)
        context.Contracts.Add(new Contract
        {
            Id = 2,
            ContractCode = "CTR-002",
            RoomId = 11,
            TenantId = 202,
            LandlordId = landlordId,
            StartDate = today,
            EndDate = today.AddMonths(6),
            MonthlyRent = 4000000,
            DepositAmount = 4000000,
            Status = ContractStatus.DRAFT
        });

        // Hợp đồng của chủ nhà khác
        context.Contracts.Add(new Contract
        {
            Id = 3,
            ContractCode = "CTR-003",
            RoomId = 12,
            TenantId = 203,
            LandlordId = 999, // Other landlord
            StartDate = today,
            EndDate = today.AddMonths(6),
            MonthlyRent = 6000000,
            DepositAmount = 6000000,
            Status = ContractStatus.ACTIVE
        });

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetLandlordTenantsAsync(landlordId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        var tenant = result.Value.First();
        Assert.Equal(201, tenant.TenantId);
        Assert.Equal(TenantLifecycleStatus.RENTING, tenant.LifecycleStatus);
    }

    #endregion

    #region 5 to 14: Soạn và quản lý vòng đời hợp đồng (UC3.7.3)

    [Fact]
    public async Task CreateContractDraft_WhenLandlordNotOwnerOfRoom_ReturnsForbidden()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        // Không đăng ký ownership cho landlord 1 trên room 100
        var service = new LeasingService(context, mockRoomService);

        var request = new CreateContractDraftRequest(
            RoomId: 100,
            TenantId: 201,
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            MonthlyRent: 3500000,
            DepositAmount: 3500000
        );

        // Act
        var result = await service.CreateContractDraftAsync(landlordUserId: 1, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Contract.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task FullContractLifecycle_TwoPartyConfirmation_SucceedsAndSetsRoomRented()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        const long landlordId = 10;
        const long tenantId = 20;
        const long roomId = 101;
        mockRoomService.RegisterOwnership(landlordId, roomId);

        var service = new LeasingService(context, mockRoomService);

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddMonths(12);
        const string initialTerms = "Rent:5000000;Deposit:5000000;Rules:NoPets";

        // 1. Chủ nhà tạo bản nháp (DRAFT)
        var createResult = await service.CreateContractDraftAsync(
            landlordId,
            new CreateContractDraftRequest(roomId, tenantId, start, end, 5000000, 5000000, 5, initialTerms));
        Assert.True(createResult.IsSuccess);
        var contractId = createResult.Value.Id;
        Assert.Equal(ContractStatus.DRAFT, createResult.Value.Status);

        // 2. Chủ nhà gửi hợp đồng cho người thuê (PENDING_TENANT_CONFIRMATION)
        var sendResult = await service.SendContractToTenantAsync(landlordId, contractId);
        Assert.True(sendResult.IsSuccess);
        Assert.Equal(ContractStatus.PENDING_TENANT_CONFIRMATION, sendResult.Value.Status);

        // 3. Người thuê xác nhận đồng ý (PENDING_LANDLORD_CONFIRMATION)
        var tenantConfirmResult = await service.TenantConfirmOrRejectContractAsync(
            tenantId, 
            contractId, 
            new TenantConfirmContractRequest(TermsSnapshotToVerify: initialTerms, Accept: true));
        Assert.True(tenantConfirmResult.IsSuccess);
        Assert.Equal(ContractStatus.PENDING_LANDLORD_CONFIRMATION, tenantConfirmResult.Value.Status);

        // 4. Chủ nhà chốt xác nhận cuối cùng (ACTIVE)
        var finalizeResult = await service.LandlordFinalizeContractAsync(landlordId, contractId);
        Assert.True(finalizeResult.IsSuccess);
        Assert.Equal(ContractStatus.ACTIVE, finalizeResult.Value.Status);

        // Phòng phải được chuyển sang RENTED
        Assert.Equal("RENTED", mockRoomService.LastRoomStatusSet);
    }

    [Fact]
    public async Task TenantConfirm_WhenTermsModifiedStealthily_RejectsConfirmation()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        const long landlordId = 10;
        const long tenantId = 20;
        const long roomId = 101;
        mockRoomService.RegisterOwnership(landlordId, roomId);
        var service = new LeasingService(context, mockRoomService);

        var contract = new Contract
        {
            Id = 1,
            ContractCode = "CTR-SAFE",
            RoomId = roomId,
            TenantId = tenantId,
            LandlordId = landlordId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            MonthlyRent = 5000000,
            DepositAmount = 5000000,
            TermsSnapshot = "Terms_Original_Version_1",
            Status = ContractStatus.PENDING_TENANT_CONFIRMATION
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        // Act: Người thuê xác nhận nhưng bản snapshot bị sai lệch
        var result = await service.TenantConfirmOrRejectContractAsync(
            tenantId, 
            1, 
            new TenantConfirmContractRequest(TermsSnapshotToVerify: "Terms_Different_Version_2", Accept: true));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Contract.ConfirmationError", result.Error.Code);
        Assert.Contains("thay đổi", result.Error.Message);
    }

    [Fact]
    public async Task TenantConfirm_WrongTenantUserId_ReturnsForbidden()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        var service = new LeasingService(context, mockRoomService);

        context.Contracts.Add(new Contract
        {
            Id = 1,
            ContractCode = "CTR-SECURE",
            RoomId = 101,
            TenantId = 20, // Real tenant
            LandlordId = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            MonthlyRent = 5000000,
            DepositAmount = 5000000,
            TermsSnapshot = "Terms_V1",
            Status = ContractStatus.PENDING_TENANT_CONFIRMATION
        });
        await context.SaveChangesAsync();

        // Act: User 999 cố tình xác nhận hợp đồng của Tenant 20
        var result = await service.TenantConfirmOrRejectContractAsync(
            tenantUserId: 999, 
            contractId: 1, 
            new TenantConfirmContractRequest("Terms_V1", true));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Contract.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task LandlordFinalize_WhenRoomAlreadyHasActiveContractDuringOverlap_RejectsActivation()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        const long landlordId = 10;
        const long roomId = 101;
        mockRoomService.RegisterOwnership(landlordId, roomId);
        var service = new LeasingService(context, mockRoomService);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Hợp đồng 1 đã ACTIVE
        context.Contracts.Add(new Contract
        {
            Id = 1,
            ContractCode = "CTR-ACTIVE",
            RoomId = roomId,
            TenantId = 20,
            LandlordId = landlordId,
            StartDate = today,
            EndDate = today.AddMonths(6),
            MonthlyRent = 5000000,
            DepositAmount = 5000000,
            Status = ContractStatus.ACTIVE
        });

        // Hợp đồng 2 cùng phòng, cùng thời gian nhưng đang PENDING_LANDLORD_CONFIRMATION
        context.Contracts.Add(new Contract
        {
            Id = 2,
            ContractCode = "CTR-PENDING",
            RoomId = roomId,
            TenantId = 21,
            LandlordId = landlordId,
            StartDate = today.AddMonths(1),
            EndDate = today.AddMonths(7),
            MonthlyRent = 5500000,
            DepositAmount = 5500000,
            Status = ContractStatus.PENDING_LANDLORD_CONFIRMATION
        });

        await context.SaveChangesAsync();

        // Act: Cố kích hoạt Hợp đồng 2 khi Hợp đồng 1 đang chiếm phòng
        var result = await service.LandlordFinalizeContractAsync(landlordId, 2);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Contract.RoomAlreadyLeased", result.Error.Code);
    }

    [Fact]
    public async Task UpdateContractDraft_WhenStatusNotDraft_Fails()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var mockRoomService = new MockRoomOwnershipService();
        var service = new LeasingService(context, mockRoomService);

        context.Contracts.Add(new Contract
        {
            Id = 1,
            ContractCode = "CTR-SENT",
            RoomId = 101,
            TenantId = 20,
            LandlordId = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            MonthlyRent = 5000000,
            DepositAmount = 5000000,
            Status = ContractStatus.PENDING_TENANT_CONFIRMATION
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateContractDraftAsync(
            landlordUserId: 10,
            contractId: 1,
            new UpdateContractDraftRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                6000000,
                6000000));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Contract.InvalidOperation", result.Error.Code);
    }

    #endregion
}
