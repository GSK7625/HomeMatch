using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Modules.Billing.Infrastructure.Services;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Leasing;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Messaging;
using HomeMatch.Shared.Contracts.Properties;
using HomeMatch.Shared.Contracts.Viewings;

namespace HomeMatch.UnitTests;

public class LandlordDashboardServiceTests
{
    private static readonly DateOnly FixedToday = new(2026, 11, 12);

    private static (
        LandlordDashboardService Service,
        FakeInvoiceRepository Invoices,
        FakePropertyDashboardReader Properties,
        FakeLeasingContractReader Contracts,
        FakeListingDashboardReader Listings,
        FakeViewingDashboardReader Viewings,
        FakeMessagingDashboardReader Messaging,
        FakeIdentityUserReader Users) CreateSut()
    {
        var invoices = new FakeInvoiceRepository();
        var properties = new FakePropertyDashboardReader();
        var contracts = new FakeLeasingContractReader();
        var listings = new FakeListingDashboardReader();
        var viewings = new FakeViewingDashboardReader();
        var messaging = new FakeMessagingDashboardReader();
        var users = new FakeIdentityUserReader();

        var service = new LandlordDashboardService(
            invoices,
            properties,
            contracts,
            listings,
            viewings,
            messaging,
            users,
            () => FixedToday);

        return (service, invoices, properties, contracts, listings, viewings, messaging, users);
    }

    private static Invoice CreateInvoice(
        long id,
        string code,
        long landlordId,
        long tenantId,
        long roomId,
        long propertyId,
        int month,
        int year,
        long rent,
        long elec,
        long water,
        long service,
        long paid,
        DateOnly dueDate,
        string status)
    {
        var total = rent + elec + water + service;
        return new Invoice
        {
            Id = id,
            InvoiceCode = code,
            ContractId = id,
            LandlordId = landlordId,
            TenantId = tenantId,
            RoomId = roomId,
            PropertyId = propertyId,
            BillingMonth = month,
            BillingYear = year,
            RentAmount = rent,
            ElectricityAmount = elec,
            WaterAmount = water,
            ServiceAmount = service,
            DiscountAmount = 0,
            TotalAmount = total,
            PaidAmount = paid,
            DueDate = dueDate,
            Status = status
        };
    }

    [Fact]
    public async Task GetDashboardAsync_WithAllBuildings_ShouldAggregateRealModuleDataAccurately()
    {
        // Arrange
        var (service, invoices, properties, contracts, listings, viewings, messaging, users) = CreateSut();

        properties.Rooms.AddRange(
        [
            new DashboardRoomInfo(101, 1, "HomeMatch Green", "P.101", "RENTED"),
            new DashboardRoomInfo(102, 1, "HomeMatch Green", "P.102", "AVAILABLE"),
            new DashboardRoomInfo(201, 2, "Sunrise House", "P.201", "RENTED"),
            new DashboardRoomInfo(202, 2, "Sunrise House", "P.202", "AVAILABLE")
        ]);

        listings.Listings.AddRange(
        [
            new DashboardListingInfo(10, 102, 2, "Studio Cầu Giấy", "PUBLISHED", 120),
            new DashboardListingInfo(11, 202, 2, "1N1K Đống Đa", "PUBLISHED", 80),
            new DashboardListingInfo(12, 101, 2, "Phòng đã thuê", "HIDDEN", 50)
        ]);

        contracts.Contracts.AddRange(
        [
            new LeasingContractInfo(1, "HD-2026-001", 101, 301, 2, new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 30), 4_800_000, 5_000_000, 5, "ACTIVE"),
            new LeasingContractInfo(2, "HD-2026-002", 201, 302, 2, new DateOnly(2026, 6, 1), new DateOnly(2027, 6, 1), 6_000_000, 6_000_000, 5, "ACTIVE")
        ]);

        invoices.Invoices.AddRange(
        [
            CreateInvoice(1, "INV-202611-001", 2, 301, 101, 1, 11, 2026, 4_800_000, 300_000, 100_000, 200_000, 5_400_000, new DateOnly(2026, 11, 5), InvoiceStatus.Paid),
            CreateInvoice(2, "INV-202611-002", 2, 302, 201, 2, 11, 2026, 6_000_000, 400_000, 100_000, 200_000, 2_000_000, new DateOnly(2026, 11, 2), InvoiceStatus.Overdue),
            CreateInvoice(3, "INV-202611-003", 2, 302, 201, 2, 11, 2026, 5_000_000, 0, 0, 0, 0, new DateOnly(2026, 11, 5), InvoiceStatus.Cancelled)
        ]);

        // 2026-11-12 10:30+07:00 = 2026-11-12 03:30 UTC
        viewings.Viewings.AddRange(
        [
            new DashboardViewingInfo(501, 10, 303, new DateTimeOffset(2026, 11, 12, 3, 30, 0, TimeSpan.Zero), IsVirtual: false, "CONFIRMED"),
            new DashboardViewingInfo(502, 11, 304, new DateTimeOffset(2026, 11, 12, 7, 0, 0, TimeSpan.Zero), IsVirtual: true, "PENDING"),
            new DashboardViewingInfo(503, 10, 303, new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero), IsVirtual: false, "CANCELLED"),
            new DashboardViewingInfo(504, 10, 303, new DateTimeOffset(2026, 11, 13, 3, 0, 0, TimeSpan.Zero), IsVirtual: false, "CONFIRMED")
        ]);

        messaging.Stats = new DashboardLeadFunnelStats(TotalLeads: 15, ContactedLeads: 9, ViewingScheduledLeads: 4, ContractSignedLeads: 2);

        users.Users[301] = new IdentityUserInfo(301, "Nguyễn Văn An", "an@test.vn", "0901111111", "Tenant");
        users.Users[302] = new IdentityUserInfo(302, "Trần Thị Bình", "binh@test.vn", "0902222222", "Tenant");
        users.Users[303] = new IdentityUserInfo(303, "Lê Minh Châu", "chau@test.vn", "0903333333", "Tenant");
        users.Users[304] = new IdentityUserInfo(304, "Phạm Đức Dũng", "dung@test.vn", "0904444444", "Tenant");

        // Act
        var result = await service.GetDashboardAsync(landlordId: 2, buildingId: "all", period: "11/2026");

        // Assert Overview
        Assert.Equal(4, result.Overview.TotalRooms);
        Assert.Equal(2, result.Overview.VacantRooms);
        Assert.Equal(2, result.Overview.RentedRooms);
        Assert.Equal(2, result.Overview.ActiveListings);
        Assert.Equal(50.0, result.Overview.OccupancyRate);

        // Assert Financials (cancelled invoice excluded)
        Assert.Equal(12_100_000m, result.Financials.TotalReceivables);
        Assert.Equal(7_400_000m, result.Financials.TotalCollected);
        Assert.Equal(4_700_000m, result.Financials.RemainingDebt);
        Assert.Equal(10_800_000m, result.Financials.Breakdown.RentAmount);
        Assert.Equal(700_000m, result.Financials.Breakdown.ElectricityAmount);
        Assert.Equal(200_000m, result.Financials.Breakdown.WaterAmount);
        Assert.Equal(400_000m, result.Financials.Breakdown.ServiceAmount);
        Assert.Equal(61.2, result.Financials.CollectionRate);

        // Assert Deposits
        Assert.Equal(11_000_000m, result.Deposits.TotalActiveDeposits);
        Assert.Equal(2, result.Deposits.ActiveContractsCount);
        Assert.Equal("HD-2026-001", result.Deposits.Items[0].ContractCode);
        Assert.Equal("Sắp đáo hạn", result.Deposits.Items[0].DepositStatus);
        Assert.Equal("P.101 - HomeMatch Green", result.Deposits.Items[0].RoomName);
        Assert.Equal("Nguyễn Văn An", result.Deposits.Items[0].TenantName);
        Assert.Equal("Đang giữ", result.Deposits.Items[1].DepositStatus);

        // Assert Viewings Today (excludes cancelled & tomorrow)
        Assert.Equal(2, result.ViewingsToday.Count);
        Assert.Equal("10:30", result.ViewingsToday[0].ScheduledTime);
        Assert.Equal("Trực tiếp", result.ViewingsToday[0].ViewingType);
        Assert.Equal("P.102 - HomeMatch Green", result.ViewingsToday[0].RoomName);
        Assert.Equal("14:00", result.ViewingsToday[1].ScheduledTime);
        Assert.Equal("Online", result.ViewingsToday[1].ViewingType);

        // Assert Marketing Funnel
        Assert.Equal(2, result.MarketingAndLeads.ActiveListingsCount);
        Assert.Equal(250, result.MarketingAndLeads.TotalViewsCount);
        Assert.Equal(15, result.MarketingAndLeads.NewLeads);
        Assert.Equal(9, result.MarketingAndLeads.ContactedLeads);
        Assert.Equal(4, result.MarketingAndLeads.ViewingScheduledLeads);
        Assert.Equal(2, result.MarketingAndLeads.ContractSignedLeads);
    }

    [Fact]
    public async Task GetDashboardAsync_WithSpecificBuilding_ShouldFilterCorrectly()
    {
        // Arrange
        var (service, invoices, properties, contracts, listings, viewings, messaging, users) = CreateSut();

        properties.Rooms.AddRange(
        [
            new DashboardRoomInfo(101, 1, "HomeMatch Green", "P.101", "RENTED"),
            new DashboardRoomInfo(102, 1, "HomeMatch Green", "P.102", "AVAILABLE"),
            new DashboardRoomInfo(201, 2, "Sunrise House", "P.201", "RENTED")
        ]);

        listings.Listings.AddRange(
        [
            new DashboardListingInfo(10, 102, 2, "Studio P.102", "PUBLISHED", 90),
            new DashboardListingInfo(20, 201, 2, "Phòng P.201", "PUBLISHED", 40)
        ]);

        contracts.Contracts.AddRange(
        [
            new LeasingContractInfo(1, "HD-1", 101, 301, 2, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 4_500_000, 5_000_000, 5, "ACTIVE"),
            new LeasingContractInfo(2, "HD-2", 201, 302, 2, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 7_000_000, 7_000_000, 5, "ACTIVE")
        ]);

        invoices.Invoices.AddRange(
        [
            CreateInvoice(1, "INV-1", 2, 301, 101, 1, 11, 2026, 4_500_000, 0, 0, 0, 0, new DateOnly(2026, 11, 5), InvoiceStatus.Overdue),
            CreateInvoice(2, "INV-2", 2, 302, 201, 2, 11, 2026, 7_000_000, 0, 0, 0, 0, new DateOnly(2026, 11, 5), InvoiceStatus.Overdue)
        ]);

        users.Users[301] = new IdentityUserInfo(301, "Nguyễn Văn An", "an@test.vn", "0901111111", "Tenant");

        // Act
        var result = await service.GetDashboardAsync(landlordId: 2, buildingId: "1", period: "11/2026");

        // Assert
        Assert.Equal(2, result.Overview.TotalRooms);
        Assert.Equal(1, result.Overview.VacantRooms);
        Assert.Equal(1, result.Overview.RentedRooms);
        Assert.Equal(1, result.Overview.ActiveListings);
        Assert.Equal(4_500_000m, result.Financials.TotalReceivables);
        Assert.Single(result.DebtAging.Items);
        Assert.Equal("P.101 - HomeMatch Green", result.DebtAging.Items[0].RoomName);
        Assert.Single(result.Deposits.Items);
        Assert.Equal(5_000_000m, result.Deposits.TotalActiveDeposits);
        Assert.Equal(90, result.MarketingAndLeads.TotalViewsCount);
    }

    [Fact]
    public async Task GetDashboardAsync_DebtAgingBuckets_ShouldSumAndSortCorrectly()
    {
        // Arrange
        var (service, invoices, properties, _, _, _, _, _) = CreateSut();

        properties.Rooms.Add(new DashboardRoomInfo(101, 1, "Tòa A", "P.101", "RENTED"));

        invoices.Invoices.AddRange(
        [
            // Trong hạn: due 2026-11-20 (today = 2026-11-12) -> 1,000,000
            CreateInvoice(1, "INV-CUR", 2, 301, 101, 1, 11, 2026, 1_000_000, 0, 0, 0, 0, new DateOnly(2026, 11, 20), InvoiceStatus.Unpaid),
            // 1-30 ngày: due 2026-11-01 (11 days overdue) -> 2,000,000
            CreateInvoice(2, "INV-15D", 2, 301, 101, 1, 11, 2026, 2_000_000, 0, 0, 0, 0, new DateOnly(2026, 11, 1), InvoiceStatus.Overdue),
            // 31-60 ngày: due 2026-10-01 (42 days overdue) -> 3,000,000
            CreateInvoice(3, "INV-42D", 2, 301, 101, 1, 11, 2026, 3_000_000, 0, 0, 0, 0, new DateOnly(2026, 10, 1), InvoiceStatus.Overdue),
            // >60 ngày: due 2026-08-15 (89 days overdue) -> 4,000,000
            CreateInvoice(4, "INV-89D", 2, 301, 101, 1, 11, 2026, 4_000_000, 0, 0, 0, 0, new DateOnly(2026, 8, 15), InvoiceStatus.Overdue),
            // Đã thanh toán đủ -> không nằm trong công nợ
            CreateInvoice(5, "INV-PAID", 2, 301, 101, 1, 11, 2026, 5_000_000, 0, 0, 0, 5_000_000, new DateOnly(2026, 11, 5), InvoiceStatus.Paid)
        ]);

        // Act
        var result = await service.GetDashboardAsync(landlordId: 2, buildingId: "all", period: "11/2026");

        // Assert
        Assert.Equal(1_000_000m, result.DebtAging.CurrentAmount);
        Assert.Equal(2_000_000m, result.DebtAging.Overdue1To30Days);
        Assert.Equal(3_000_000m, result.DebtAging.Overdue31To60Days);
        Assert.Equal(4_000_000m, result.DebtAging.OverdueOver60Days);
        Assert.Equal(9_000_000m, result.DebtAging.OverdueDebt);
        Assert.Equal(10_000_000m, result.DebtAging.TotalDebt);
        Assert.Equal(4, result.DebtAging.Items.Count);
        Assert.Equal("INV-89D", result.DebtAging.Items[0].InvoiceId);
        Assert.Equal(">60 ngày", result.DebtAging.Items[0].AgingBucket);
        Assert.Equal("INV-CUR", result.DebtAging.Items[3].InvoiceId);
        Assert.Equal("Trong hạn", result.DebtAging.Items[3].AgingBucket);
    }

    [Fact]
    public async Task GetDashboardAsync_WithInvalidNonNumericBuildingId_ShouldReturnEmptyDashboard()
    {
        var (service, _, properties, _, _, _, _, _) = CreateSut();
        properties.Rooms.Add(new DashboardRoomInfo(101, 1, "Tòa A", "P.101", "RENTED"));

        var result = await service.GetDashboardAsync(landlordId: 2, buildingId: "bld-invalid", period: "11/2026");

        Assert.Equal(0, result.Overview.TotalRooms);
        Assert.Equal(0m, result.Financials.TotalReceivables);
        Assert.Empty(result.DebtAging.Items);
        Assert.Empty(result.Deposits.Items);
        Assert.Empty(result.ViewingsToday);
    }

    // --- Lightweight Fakes ---

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public List<Invoice> Invoices { get; } = [];

        public Task<Invoice?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invoices.FirstOrDefault(i => i.Id == id));

        public Task<Invoice?> GetByIdWithPaymentsAsync(long id, CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<Invoice?> GetByCodeAsync(string invoiceCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invoices.FirstOrDefault(i => i.InvoiceCode == invoiceCode));

        public Task<bool> HasActiveInvoiceForPeriodAsync(long contractId, int billingMonth, int billingYear, CancellationToken cancellationToken = default) =>
            Task.FromResult(Invoices.Any(i => i.ContractId == contractId && i.BillingMonth == billingMonth && i.BillingYear == billingYear && i.Status != InvoiceStatus.Cancelled));

        public Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(InvoiceFilter filter, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Invoice>, int)>((Invoices, Invoices.Count));

        public Task<IReadOnlyList<Invoice>> GetForDashboardAsync(
            long? landlordId,
            long? propertyId,
            int? billingMonth,
            int? billingYear,
            CancellationToken cancellationToken = default)
        {
            var query = Invoices.Where(i => i.Status != InvoiceStatus.Cancelled);
            if (landlordId.HasValue) query = query.Where(i => i.LandlordId == landlordId.Value);
            if (propertyId.HasValue) query = query.Where(i => i.PropertyId == propertyId.Value);
            if (billingMonth.HasValue && billingYear.HasValue)
            {
                query = query.Where(i => i.BillingMonth == billingMonth.Value && i.BillingYear == billingYear.Value);
            }
            return Task.FromResult<IReadOnlyList<Invoice>>(query.ToList());
        }

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            Invoices.Add(invoice);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakePropertyDashboardReader : IPropertyDashboardReader
    {
        public List<DashboardRoomInfo> Rooms { get; } = [];

        public Task<IReadOnlyList<DashboardRoomInfo>> GetRoomsForDashboardAsync(
            long? landlordId,
            long? propertyId = null,
            CancellationToken cancellationToken = default)
        {
            var query = Rooms.AsEnumerable();
            if (propertyId.HasValue) query = query.Where(r => r.PropertyId == propertyId.Value);
            return Task.FromResult<IReadOnlyList<DashboardRoomInfo>>(query.ToList());
        }
    }

    private sealed class FakeLeasingContractReader : ILeasingContractReader
    {
        public List<LeasingContractInfo> Contracts { get; } = [];

        public Task<LeasingContractInfo?> GetContractByIdAsync(long contractId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Contracts.FirstOrDefault(c => c.Id == contractId));

        public Task<IReadOnlyList<LeasingContractInfo>> GetActiveContractsByLandlordIdAsync(long landlordId, CancellationToken cancellationToken = default) =>
            GetActiveContractsAsync(landlordId, cancellationToken);

        public Task<IReadOnlyList<LeasingContractInfo>> GetActiveContractsAsync(long? landlordId = null, CancellationToken cancellationToken = default)
        {
            var query = Contracts.Where(c => c.Status == "ACTIVE");
            if (landlordId.HasValue) query = query.Where(c => c.LandlordId == landlordId.Value);
            return Task.FromResult<IReadOnlyList<LeasingContractInfo>>(query.ToList());
        }

        public Task<long> GetTotalDepositAmountAsync(long landlordId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Contracts.Where(c => c.LandlordId == landlordId && c.Status == "ACTIVE").Sum(c => c.DepositAmount));
    }

    private sealed class FakeListingDashboardReader : IListingDashboardReader
    {
        public List<DashboardListingInfo> Listings { get; } = [];

        public Task<IReadOnlyList<DashboardListingInfo>> GetListingsForDashboardAsync(
            long? landlordId,
            IReadOnlyCollection<long>? roomIds = null,
            CancellationToken cancellationToken = default)
        {
            if (roomIds is not null && roomIds.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<DashboardListingInfo>>([]);
            }

            var query = Listings.AsEnumerable();
            if (landlordId.HasValue) query = query.Where(l => l.LandlordId == landlordId.Value);
            if (roomIds is not null) query = query.Where(l => roomIds.Contains(l.RoomId));
            return Task.FromResult<IReadOnlyList<DashboardListingInfo>>(query.ToList());
        }
    }

    private sealed class FakeViewingDashboardReader : IViewingDashboardReader
    {
        public List<DashboardViewingInfo> Viewings { get; } = [];

        public Task<IReadOnlyList<DashboardViewingInfo>> GetViewingsByListingIdsAsync(
            IReadOnlyCollection<long> listingIds,
            CancellationToken cancellationToken = default)
        {
            if (listingIds.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<DashboardViewingInfo>>([]);
            }

            return Task.FromResult<IReadOnlyList<DashboardViewingInfo>>(
                Viewings.Where(v => listingIds.Contains(v.ListingId)).ToList());
        }
    }

    private sealed class FakeMessagingDashboardReader : IMessagingDashboardReader
    {
        public DashboardLeadFunnelStats Stats { get; set; } = new(0, 0, 0, 0);

        public Task<DashboardLeadFunnelStats> GetLeadFunnelByListingIdsAsync(
            IReadOnlyCollection<long> listingIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(listingIds.Count == 0 ? new DashboardLeadFunnelStats(0, 0, 0, 0) : Stats);
    }

    private sealed class FakeIdentityUserReader : IIdentityUserReader
    {
        public Dictionary<long, IdentityUserInfo> Users { get; } = [];

        public Task<IdentityUserInfo?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.TryGetValue(userId, out var user) ? user : null);

        public Task<IReadOnlyList<IdentityUserInfo>> GetTenantsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IdentityUserInfo>>(Users.Values.ToList());
    }
}
