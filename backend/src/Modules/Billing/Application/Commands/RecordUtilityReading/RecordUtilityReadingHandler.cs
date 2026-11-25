using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Leasing;

namespace HomeMatch.Modules.Billing.Application.Commands.RecordUtilityReading;

public class RecordUtilityReadingHandler
{
    private readonly IUtilityReadingRepository _readingRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILeasingContractReader _leasingContractReader;
    private readonly IRoomListingReader _roomListingReader;

    public RecordUtilityReadingHandler(
        IUtilityReadingRepository readingRepository,
        IInvoiceRepository invoiceRepository,
        ILeasingContractReader leasingContractReader,
        IRoomListingReader roomListingReader)
    {
        _readingRepository = readingRepository;
        _invoiceRepository = invoiceRepository;
        _leasingContractReader = leasingContractReader;
        _roomListingReader = roomListingReader;
    }

    public async Task<Result<UtilityReadingDto>> HandleAsync(
        RecordUtilityReadingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.BillingMonth < 1 || command.BillingMonth > 12)
        {
            return Result.Failure<UtilityReadingDto>(new Error("Billing.InvalidMonth", "Kỳ tính tiền tháng phải từ 1 đến 12."));
        }

        if (command.BillingYear < 2020)
        {
            return Result.Failure<UtilityReadingDto>(new Error("Billing.InvalidYear", "Kỳ tính tiền năm không hợp lệ."));
        }

        // 1. Lấy hợp đồng từ module Leasing
        var contract = await _leasingContractReader.GetContractByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<UtilityReadingDto>(new Error("Billing.ContractNotFound", "Không tìm thấy thông tin hợp đồng thuê yêu cầu."));
        }

        // 2. Kiểm tra quyền sở hữu: Chỉ chủ nhà của hợp đồng hoặc Admin
        if (!command.IsAdmin && contract.LandlordId != command.RequestUserId)
        {
            return Result.Failure<UtilityReadingDto>(BillingErrors.Forbidden);
        }

        // 2.1. Kiểm tra trạng thái hợp đồng và phạm vi kỳ thuê hợp lệ (BR-BIL-02, BR-BIL-05)
        bool isBillableStatus =
            string.Equals(contract.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contract.Status, "EXPIRED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contract.Status, "EXPIRED_PENDING_HANDOVER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contract.Status, "HANDOVER_OVERDUE", StringComparison.OrdinalIgnoreCase);

        if (!isBillableStatus)
        {
            return Result.Failure<UtilityReadingDto>(BillingErrors.ContractNotActive);
        }

        int daysInMonth = DateTime.DaysInMonth(command.BillingYear, command.BillingMonth);
        var startOfMonth = new DateOnly(command.BillingYear, command.BillingMonth, 1);
        var endOfMonth = new DateOnly(command.BillingYear, command.BillingMonth, daysInMonth);
        bool periodOverlapsContract = !(contract.EndDate < startOfMonth || contract.StartDate > endOfMonth);

        if (!periodOverlapsContract)
        {
            return Result.Failure<UtilityReadingDto>(BillingErrors.ContractExpiredPeriodInvalid);
        }

        // 3. Kiểm tra quy tắc BR-BIL-13: Nếu đã có hóa đơn active (chưa hủy) cho kỳ này thì không cho sửa chỉ số điện nước
        bool hasActiveInvoice = await _invoiceRepository.HasActiveInvoiceForPeriodAsync(
            command.ContractId, command.BillingMonth, command.BillingYear, cancellationToken);
        if (hasActiveInvoice)
        {
            return Result.Failure<UtilityReadingDto>(BillingErrors.CannotModifyReadingWithActiveInvoice);
        }

        // 4. Lấy thông tin phòng và đơn giá hiệu lực (ưu tiên cấp phòng, fallback cấp tòa nhà) từ Properties module
        var roomSnapshot = await _roomListingReader.GetRoomAsync(contract.RoomId, cancellationToken);
        long propertyId = roomSnapshot?.PropertyId ?? 0;

        var tariffs = await _roomListingReader.GetEffectiveRoomTariffsAsync(contract.RoomId, cancellationToken);
        var elecTariff = tariffs.FirstOrDefault(t => string.Equals(t.TariffType, "ELECTRICITY", StringComparison.OrdinalIgnoreCase));
        var waterTariff = tariffs.FirstOrDefault(t => string.Equals(t.TariffType, "WATER", StringComparison.OrdinalIgnoreCase));

        long elecUnitPrice = command.ElectricityUnitPriceOverride
            ?? (elecTariff is not null && !string.Equals(elecTariff.CalcMethod, "FREE", StringComparison.OrdinalIgnoreCase)
                ? elecTariff.UnitPrice
                : 0);

        long waterUnitPrice = command.WaterUnitPriceOverride
            ?? (waterTariff is not null && !string.Equals(waterTariff.CalcMethod, "FREE", StringComparison.OrdinalIgnoreCase)
                ? waterTariff.UnitPrice
                : 0);

        long serviceCost;
        if (command.ServiceCostOverride.HasValue)
        {
            serviceCost = command.ServiceCostOverride.Value;
        }
        else
        {
            serviceCost = tariffs
                .Where(t => !string.Equals(t.TariffType, "ELECTRICITY", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(t.TariffType, "WATER", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(t.CalcMethod, "FREE", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.UnitPrice);
        }

        // 5. Tìm bản ghi hiện có của phòng trong kỳ (nếu có thì cập nhật, chưa có thì tạo mới)
        var existing = await _readingRepository.GetByRoomAndPeriodAsync(
            contract.RoomId, command.BillingMonth, command.BillingYear, cancellationToken);

        var readingDate = command.ReadingDate ?? BusinessDate.Today();

        var readingResult = UtilityReading.CreateOrUpdate(
            existing,
            propertyId,
            contract.RoomId,
            contract.Id,
            contract.LandlordId,
            contract.TenantId,
            command.RequestUserId,
            command.BillingMonth,
            command.BillingYear,
            command.ElectricityOld,
            command.ElectricityNew,
            elecUnitPrice,
            command.WaterOld,
            command.WaterNew,
            waterUnitPrice,
            serviceCost,
            readingDate);

        if (readingResult.IsFailure)
        {
            return Result.Failure<UtilityReadingDto>(readingResult.Error);
        }

        var reading = readingResult.Value;
        if (existing is null)
        {
            await _readingRepository.AddAsync(reading, cancellationToken);
        }
        else
        {
            await _readingRepository.UpdateAsync(reading, cancellationToken);
        }

        return Result.Success(ToDto(reading));
    }

    public static UtilityReadingDto ToDto(UtilityReading u) =>
        new(
            u.Id,
            u.PropertyId,
            u.RoomId,
            u.ContractId,
            u.LandlordId,
            u.TenantId,
            u.RecordedBy,
            u.BillingMonth,
            u.BillingYear,
            u.ElectricityOld,
            u.ElectricityNew,
            u.ElectricityUsage,
            u.ElectricityUnitPrice,
            u.ElectricityCost,
            u.WaterOld,
            u.WaterNew,
            u.WaterUsage,
            u.WaterUnitPrice,
            u.WaterCost,
            u.ServiceCost,
            u.ReadingDate,
            u.CreatedAt,
            u.UpdatedAt);
}
