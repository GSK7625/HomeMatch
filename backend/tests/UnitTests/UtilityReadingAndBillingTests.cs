using HomeMatch.Modules.Billing.Application.Commands.CreateInvoice;
using HomeMatch.Modules.Billing.Application.Commands.RecordUtilityReading;
using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Leasing;

namespace HomeMatch.UnitTests;

public class UtilityReadingAndBillingTests
{
    [Fact]
    public async Task RecordUtilityReading_And_CreateInvoice_ShouldCalculateConsumptionAndTotalAmountCorrectly()
    {
        // Arrange
        var readingRepo = new FakeUtilityReadingRepository();
        var invoiceRepo = new FakeInvoiceRepository();
        var contractReader = new FakeLeasingContractReader();
        var roomReader = new FakeRoomListingReader();

        var today = BusinessDate.Today();
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        contractReader.Contracts.Add(new LeasingContractInfo(
            Id: 10,
            ContractCode: "HD-2026-010",
            RoomId: 101,
            TenantId: 301,
            LandlordId: 2,
            StartDate: startOfMonth,
            EndDate: endOfMonth,
            MonthlyRent: 5_000_000,
            DepositAmount: 5_000_000,
            BillingDueDay: 5,
            Status: "ACTIVE"));

        roomReader.Tariffs.AddRange(
        [
            new RoomListingTariffSnapshot(1, "ELECTRICITY", "PER_KWH", 3_800),
            new RoomListingTariffSnapshot(2, "WATER", "PER_M3", 25_000),
            new RoomListingTariffSnapshot(3, "INTERNET", "FIXED_PER_MONTH", 100_000),
            new RoomListingTariffSnapshot(4, "SERVICE", "FIXED_PER_MONTH", 150_000)
        ]);

        var recordHandler = new RecordUtilityReadingHandler(readingRepo, invoiceRepo, contractReader, roomReader);
        var createInvoiceHandler = new CreateInvoiceHandler(invoiceRepo, contractReader, roomReader, readingRepo);

        // Act 1: Ghi chỉ số điện nước (Điện: 120 -> 220 = 100 kWh; Nước: 40 -> 48 = 8 m3)
        var recordCmd = new RecordUtilityReadingCommand(
            ContractId: 10,
            BillingMonth: today.Month,
            BillingYear: today.Year,
            ElectricityOld: 120,
            ElectricityNew: 220,
            WaterOld: 40,
            WaterNew: 48,
            ElectricityUnitPriceOverride: null,
            WaterUnitPriceOverride: null,
            ServiceCostOverride: null,
            ReadingDate: today,
            RequestUserId: 2,
            IsAdmin: false);

        var readingRes = await recordHandler.HandleAsync(recordCmd);

        // Assert 1: Kiểm tra lượng tiêu thụ và tiền điện nước
        Assert.True(readingRes.IsSuccess);
        Assert.Equal(100, readingRes.Value.ElectricityUsage);
        Assert.Equal(380_000, readingRes.Value.ElectricityCost);
        Assert.Equal(8, readingRes.Value.WaterUsage);
        Assert.Equal(200_000, readingRes.Value.WaterCost);
        Assert.Equal(250_000, readingRes.Value.ServiceCost);

        // Act 2: Phát hành hóa đơn cho kỳ đó
        var createInvCmd = new CreateInvoiceCommand(
            ContractId: 10,
            BillingMonth: today.Month,
            BillingYear: today.Year,
            RequestUserId: 2,
            IsAdmin: false);

        var invoiceRes = await createInvoiceHandler.HandleAsync(createInvCmd);

        // Assert 2: Hóa đơn tự động cộng tiền thuê + tiền điện + tiền nước + phí dịch vụ
        Assert.True(invoiceRes.IsSuccess);
        Assert.Equal(5_000_000, invoiceRes.Value.RentAmount);
        Assert.Equal(380_000, invoiceRes.Value.ElectricityAmount);
        Assert.Equal(200_000, invoiceRes.Value.WaterAmount);
        Assert.Equal(250_000, invoiceRes.Value.ServiceAmount);
        Assert.Equal(5_830_000, invoiceRes.Value.TotalAmount);

        // Act 3: Thử sửa chỉ số khi đã có hóa đơn UNPAID (BR-BIL-13 phải chặn)
        var modifyRes = await recordHandler.HandleAsync(recordCmd with { ElectricityNew = 250 });
        Assert.True(modifyRes.IsFailure);
        Assert.Equal("Billing.CannotModifyReadingWithActiveInvoice", modifyRes.Error.Code);
    }

    [Fact]
    public void UtilityReading_CreateOrUpdate_WhenNewLessThanOld_ShouldReturnFailure()
    {
        var res = UtilityReading.CreateOrUpdate(
            existing: null,
            propertyId: 1,
            roomId: 101,
            contractId: 10,
            landlordId: 2,
            tenantId: 301,
            recordedBy: 2,
            billingMonth: 11,
            billingYear: 2026,
            electricityOld: 200,
            electricityNew: 180,
            electricityUnitPrice: 3800,
            waterOld: 10,
            waterNew: 15,
            waterUnitPrice: 25000,
            serviceCost: 150000,
            readingDate: new DateOnly(2026, 11, 20));

        Assert.True(res.IsFailure);
        Assert.Equal("UtilityReading.ElectricityNewLessThanOld", res.Error.Code);
    }

    [Fact]
    public async Task RecordUtilityReading_WhenLandlordDoesNotOwnContract_ShouldReturnForbidden()
    {
        var readingRepo = new FakeUtilityReadingRepository();
        var invoiceRepo = new FakeInvoiceRepository();
        var contractReader = new FakeLeasingContractReader();
        var roomReader = new FakeRoomListingReader();

        contractReader.Contracts.Add(new LeasingContractInfo(
            Id: 10,
            ContractCode: "HD-2026-010",
            RoomId: 101,
            TenantId: 301,
            LandlordId: 2,
            StartDate: new DateOnly(2026, 11, 1),
            EndDate: new DateOnly(2026, 11, 30),
            MonthlyRent: 5_000_000,
            DepositAmount: 5_000_000,
            BillingDueDay: 5,
            Status: "ACTIVE"));

        var recordHandler = new RecordUtilityReadingHandler(readingRepo, invoiceRepo, contractReader, roomReader);

        var res = await recordHandler.HandleAsync(new RecordUtilityReadingCommand(
            ContractId: 10,
            BillingMonth: 11,
            BillingYear: 2026,
            ElectricityOld: 100,
            ElectricityNew: 150,
            WaterOld: 10,
            WaterNew: 15,
            ElectricityUnitPriceOverride: null,
            WaterUnitPriceOverride: null,
            ServiceCostOverride: null,
            ReadingDate: new DateOnly(2026, 11, 20),
            RequestUserId: 999, // Chủ nhà khác cố tình ghi đè (IDOR)
            IsAdmin: false));

        Assert.True(res.IsFailure);
        Assert.Equal("Billing.Forbidden", res.Error.Code);
    }

    [Fact]
    public async Task RecordUtilityReading_WhenContractIsDraftOrPeriodOutOfRange_ShouldFail()
    {
        var readingRepo = new FakeUtilityReadingRepository();
        var invoiceRepo = new FakeInvoiceRepository();
        var contractReader = new FakeLeasingContractReader();
        var roomReader = new FakeRoomListingReader();

        contractReader.Contracts.Add(new LeasingContractInfo(
            Id: 11,
            ContractCode: "HD-DRAFT",
            RoomId: 101,
            TenantId: 301,
            LandlordId: 2,
            StartDate: new DateOnly(2026, 11, 1),
            EndDate: new DateOnly(2026, 11, 30),
            MonthlyRent: 5_000_000,
            DepositAmount: 5_000_000,
            BillingDueDay: 5,
            Status: "DRAFT"));

        var recordHandler = new RecordUtilityReadingHandler(readingRepo, invoiceRepo, contractReader, roomReader);

        var draftRes = await recordHandler.HandleAsync(new RecordUtilityReadingCommand(
            ContractId: 11,
            BillingMonth: 11,
            BillingYear: 2026,
            ElectricityOld: 100,
            ElectricityNew: 150,
            WaterOld: 10,
            WaterNew: 15,
            ElectricityUnitPriceOverride: null,
            WaterUnitPriceOverride: null,
            ServiceCostOverride: null,
            ReadingDate: new DateOnly(2026, 11, 20),
            RequestUserId: 2,
            IsAdmin: false));

        Assert.True(draftRes.IsFailure);
        Assert.Equal("Billing.ContractNotActive", draftRes.Error.Code);
    }

    [Fact]
    public void UtilityReading_CreateOrUpdate_WhenMultiplicationOverflows_ShouldReturnFailure()
    {
        var res = UtilityReading.CreateOrUpdate(
            existing: null,
            propertyId: 1,
            roomId: 101,
            contractId: 10,
            landlordId: 2,
            tenantId: 301,
            recordedBy: 2,
            billingMonth: 11,
            billingYear: 2026,
            electricityOld: 0,
            electricityNew: 1_000_000,
            electricityUnitPrice: long.MaxValue / 10,
            waterOld: 0,
            waterNew: 10,
            waterUnitPrice: 25_000,
            serviceCost: 0,
            readingDate: new DateOnly(2026, 11, 20));

        Assert.True(res.IsFailure);
        Assert.Equal("UtilityReading.AmountOverflow", res.Error.Code);
    }

    private sealed class FakeUtilityReadingRepository : IUtilityReadingRepository
    {
        public List<UtilityReading> Readings { get; } = [];

        public Task<UtilityReading?> GetByRoomAndPeriodAsync(long roomId, int billingMonth, int billingYear, CancellationToken cancellationToken = default) =>
            Task.FromResult(Readings.FirstOrDefault(u => u.RoomId == roomId && u.BillingMonth == billingMonth && u.BillingYear == billingYear));

        public Task<UtilityReading?> GetByContractAndPeriodAsync(long contractId, int billingMonth, int billingYear, CancellationToken cancellationToken = default) =>
            Task.FromResult(Readings.FirstOrDefault(u => u.ContractId == contractId && u.BillingMonth == billingMonth && u.BillingYear == billingYear));

        public Task<UtilityReading?> GetLatestBeforePeriodAsync(long roomId, int billingMonth, int billingYear, CancellationToken cancellationToken = default) =>
            Task.FromResult(Readings
                .Where(u => u.RoomId == roomId && (u.BillingYear < billingYear || (u.BillingYear == billingYear && u.BillingMonth < billingMonth)))
                .OrderByDescending(u => u.BillingYear)
                .ThenByDescending(u => u.BillingMonth)
                .FirstOrDefault());

        public Task<IReadOnlyList<UtilityReading>> GetListAsync(UtilityReadingFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UtilityReading>>(Readings.ToList());

        public Task AddAsync(UtilityReading reading, CancellationToken cancellationToken = default)
        {
            reading.Id = Readings.Count + 1;
            Readings.Add(reading);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UtilityReading reading, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

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

        public Task<IReadOnlyList<Invoice>> GetForDashboardAsync(long? landlordId, long? propertyId, int? billingMonth, int? billingYear, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Invoice>>(Invoices);

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            invoice.Id = Invoices.Count + 1;
            Invoices.Add(invoice);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeLeasingContractReader : ILeasingContractReader
    {
        public List<LeasingContractInfo> Contracts { get; } = [];

        public Task<LeasingContractInfo?> GetContractByIdAsync(long contractId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Contracts.FirstOrDefault(c => c.Id == contractId));

        public Task<IReadOnlyList<LeasingContractInfo>> GetActiveContractsByLandlordIdAsync(long landlordId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeasingContractInfo>>(Contracts.Where(c => c.LandlordId == landlordId).ToList());

        public Task<IReadOnlyList<LeasingContractInfo>> GetActiveContractsAsync(long? landlordId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeasingContractInfo>>(Contracts);

        public Task<long> GetTotalDepositAmountAsync(long landlordId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);
    }

    private sealed class FakeRoomListingReader : IRoomListingReader
    {
        public List<RoomListingTariffSnapshot> Tariffs { get; } = [];

        public Task<IReadOnlyList<RoomListingSearchMatch>> SearchAvailableRoomsAsync(RoomListingSearchFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoomListingSearchMatch>>([]);

        public Task<RoomListingSnapshot?> GetRoomAsync(long roomId, CancellationToken cancellationToken) =>
            Task.FromResult<RoomListingSnapshot?>(new RoomListingSnapshot(roomId, 1, 2, "RENTED", "01", "001", "So 1"));

        public Task<RoomListingDetailSnapshot?> GetRoomDetailAsync(long roomId, CancellationToken cancellationToken) =>
            Task.FromResult<RoomListingDetailSnapshot?>(null);

        public Task<IReadOnlyDictionary<long, RoomListingSnapshot>> GetRoomsAsync(IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<long, RoomListingSnapshot>>(new Dictionary<long, RoomListingSnapshot>());

        public Task<IReadOnlyDictionary<long, RoomCardSummary>> GetRoomCardSummariesAsync(IReadOnlyCollection<long> roomIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<long, RoomCardSummary>>(new Dictionary<long, RoomCardSummary>());

        public Task<bool> HasElectricityAndWaterRatesAsync(long roomId, DateOnly date, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<RoomListingTariffSnapshot>> GetEffectiveRoomTariffsAsync(long roomId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoomListingTariffSnapshot>>(Tariffs);

        public Task<bool> RoomTypeExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
