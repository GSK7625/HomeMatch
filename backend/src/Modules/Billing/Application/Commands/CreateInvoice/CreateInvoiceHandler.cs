using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Services;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Leasing;

namespace HomeMatch.Modules.Billing.Application.Commands.CreateInvoice;

public class CreateInvoiceHandler
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILeasingContractReader _leasingContractReader;
    private readonly IRoomListingReader _roomListingReader;

    public CreateInvoiceHandler(
        IInvoiceRepository invoiceRepository,
        ILeasingContractReader leasingContractReader,
        IRoomListingReader roomListingReader)
    {
        _invoiceRepository = invoiceRepository;
        _leasingContractReader = leasingContractReader;
        _roomListingReader = roomListingReader;
    }

    public async Task<Result<InvoiceDto>> HandleAsync(CreateInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        if (command.BillingMonth < 1 || command.BillingMonth > 12)
        {
            return Result.Failure<InvoiceDto>(new Error("Billing.InvalidMonth", "Kỳ tính tiền tháng phải từ 1 đến 12."));
        }

        if (command.BillingYear < 2020)
        {
            return Result.Failure<InvoiceDto>(new Error("Billing.InvalidYear", "Kỳ tính tiền năm không hợp lệ."));
        }

        // 1. Lấy thông tin hợp đồng từ module Leasing
        var contract = await _leasingContractReader.GetContractByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<InvoiceDto>(new Error("Billing.ContractNotFound", "Không tìm thấy thông tin hợp đồng thuê yêu cầu."));
        }

        // 2. Kiểm tra phân quyền: Chỉ chủ nhà của hợp đồng hoặc Admin mới được tạo hóa đơn
        if (!command.IsAdmin && contract.LandlordId != command.RequestUserId)
        {
            return Result.Failure<InvoiceDto>(BillingErrors.Forbidden);
        }

        var today = BusinessDate.Today();
        bool isCurrentMonth = command.BillingMonth == today.Month && command.BillingYear == today.Year;

        int daysInMonth = DateTime.DaysInMonth(command.BillingYear, command.BillingMonth);
        var startOfMonth = new DateOnly(command.BillingYear, command.BillingMonth, 1);
        var endOfMonth = new DateOnly(command.BillingYear, command.BillingMonth, daysInMonth);
        bool periodOverlapsContract = !(contract.EndDate < startOfMonth || contract.StartDate > endOfMonth);

        if (!periodOverlapsContract)
        {
            return Result.Failure<InvoiceDto>(BillingErrors.ContractExpiredPeriodInvalid);
        }

        // 3. Kiểm tra tính hợp lệ của kỳ hóa đơn theo trạng thái hợp đồng (BR-BIL-02, BR-BIL-04, BR-BIL-05)
        if (string.Equals(contract.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            if (!isCurrentMonth && !command.IsAdmin)
            {
                return Result.Failure<InvoiceDto>(new Error(
                    "Billing.CurrentMonthOnly",
                    "Trong điều kiện thông thường, chủ nhà chỉ được tạo hóa đơn cho tháng hiện tại."));
            }
        }
        else if (string.Equals(contract.Status, "EXPIRED", StringComparison.OrdinalIgnoreCase))
        {
            // Ngoại lệ: Cho phép tạo hóa đơn tháng cuối khi hợp đồng đã hết hạn nếu kỳ thuê hợp lệ
            if (!periodOverlapsContract)
            {
                return Result.Failure<InvoiceDto>(BillingErrors.ContractExpiredPeriodInvalid);
            }
        }
        else
        {
            return Result.Failure<InvoiceDto>(BillingErrors.ContractNotActive);
        }

        // 4. Kiểm tra chống tạo hóa đơn trùng kỳ (BR-BIL-06, BR-BIL-07)
        bool hasActiveInvoice = await _invoiceRepository.HasActiveInvoiceForPeriodAsync(
            command.ContractId, command.BillingMonth, command.BillingYear, cancellationToken);
        if (hasActiveInvoice)
        {
            return Result.Failure<InvoiceDto>(BillingErrors.DuplicatePeriodInvoice);
        }

        // 5. Lấy PropertyId từ RoomListingReader
        var roomSnapshot = await _roomListingReader.GetRoomAsync(contract.RoomId, cancellationToken);
        long propertyId = roomSnapshot?.PropertyId ?? 0;

        // 6. Tính số tiền thuê (tròn tháng hoặc lẻ ngày theo BR-BIL-08, BR-BIL-10)
        long rentAmount = InvoiceCalculator.CalculateRent(
            contract.MonthlyRent,
            contract.StartDate,
            contract.EndDate,
            command.BillingMonth,
            command.BillingYear);

        // 7. Tính hạn thanh toán theo điều khoản hợp đồng (BR-BIL-11, BR-BIL-12)
        var dueDate = InvoiceCalculator.CalculateDueDate(
            contract.BillingDueDay,
            command.BillingMonth,
            command.BillingYear);

        // 8. Sinh mã hóa đơn duy nhất
        string invoiceCode = $"INV-{command.BillingYear:D4}{command.BillingMonth:D2}-{contract.Id:D4}-{Random.Shared.Next(1000, 9999)}";

        // 9. Khởi tạo thực thể Invoice
        var invoiceResult = Invoice.Create(
            invoiceCode,
            contract.Id,
            contract.LandlordId,
            contract.TenantId,
            contract.RoomId,
            propertyId,
            command.BillingMonth,
            command.BillingYear,
            rentAmount,
            dueDate);

        if (invoiceResult.IsFailure)
        {
            return Result.Failure<InvoiceDto>(invoiceResult.Error);
        }

        var invoice = invoiceResult.Value;
        await _invoiceRepository.AddAsync(invoice, cancellationToken);

        var dto = new InvoiceDto(
            invoice.Id,
            invoice.InvoiceCode,
            invoice.ContractId,
            invoice.LandlordId,
            invoice.TenantId,
            invoice.RoomId,
            invoice.PropertyId,
            invoice.BillingMonth,
            invoice.BillingYear,
            invoice.RentAmount,
            invoice.ElectricityAmount,
            invoice.WaterAmount,
            invoice.ServiceAmount,
            invoice.DiscountAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.Status,
            invoice.CancelReason,
            invoice.CancelledAt,
            invoice.CreatedAt,
            invoice.UpdatedAt);

        return Result.Success(dto);
    }
}
