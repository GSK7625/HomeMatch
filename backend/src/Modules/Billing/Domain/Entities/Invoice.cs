using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Domain.Entities;

/// <summary>
/// Thực thể Hóa đơn tiền phòng định kỳ
/// </summary>
public class Invoice
{
    public long Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;

    // Tham chiếu logic liên module (không ràng buộc FK vật lý)
    public long ContractId { get; set; }
    public long LandlordId { get; set; }
    public long TenantId { get; set; }
    public long RoomId { get; set; }
    public long PropertyId { get; set; } // Hỗ trợ lọc báo cáo theo Tòa nhà (BR-BIL-30)

    // Kỳ hóa đơn theo tháng dương lịch (BR-BIL-03)
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }

    // Khoản tiền (BR-BIL-08, BR-BIL-10)
    public long RentAmount { get; set; }
    public long ElectricityAmount { get; set; } = 0;
    public long WaterAmount { get; set; } = 0;
    public long ServiceAmount { get; set; } = 0;
    public long DiscountAmount { get; set; } = 0;
    public long TotalAmount { get; set; }
    public long PaidAmount { get; set; } = 0;

    // Hạn thanh toán & Thời điểm thanh toán thực tế (BR-BIL-11, BR-BIL-28)
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    // Trạng thái hóa đơn (Mục 5.1 BRD)
    public string Status { get; set; } = InvoiceStatus.Unpaid;

    // Hủy hóa đơn (BR-BIL-14, BR-BIL-16)
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Danh sách giao dịch thanh toán nội bộ
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>
    /// Factory tạo hóa đơn mới
    /// </summary>
    public static Result<Invoice> Create(
        string invoiceCode,
        long contractId,
        long landlordId,
        long tenantId,
        long roomId,
        long propertyId,
        int billingMonth,
        int billingYear,
        long rentAmount,
        DateOnly dueDate,
        long electricityAmount = 0,
        long waterAmount = 0,
        long serviceAmount = 0,
        long discountAmount = 0)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
        {
            return Result.Failure<Invoice>(new Error("Invoice.InvalidCode", "Mã hóa đơn không được để trống."));
        }

        if (billingMonth < 1 || billingMonth > 12)
        {
            return Result.Failure<Invoice>(new Error("Invoice.InvalidMonth", "Kỳ tính tiền tháng phải từ 1 đến 12."));
        }

        if (billingYear < 2020)
        {
            return Result.Failure<Invoice>(new Error("Invoice.InvalidYear", "Kỳ tính tiền năm không hợp lệ."));
        }

        if (rentAmount < 0)
        {
            return Result.Failure<Invoice>(new Error("Invoice.InvalidRent", "Tiền thuê phòng không được là số âm."));
        }

        var total = rentAmount + electricityAmount + waterAmount + serviceAmount - discountAmount;
        if (total < 0)
        {
            total = 0;
        }

        var invoice = new Invoice
        {
            InvoiceCode = invoiceCode,
            ContractId = contractId,
            LandlordId = landlordId,
            TenantId = tenantId,
            RoomId = roomId,
            PropertyId = propertyId,
            BillingMonth = billingMonth,
            BillingYear = billingYear,
            RentAmount = rentAmount,
            ElectricityAmount = electricityAmount,
            WaterAmount = waterAmount,
            ServiceAmount = serviceAmount,
            DiscountAmount = discountAmount,
            TotalAmount = total,
            PaidAmount = 0,
            DueDate = dueDate,
            Status = InvoiceStatus.Unpaid,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(invoice);
    }

    /// <summary>
    /// Kiểm tra điều kiện cho phép hủy hóa đơn (BR-BIL-14, BR-BIL-15)
    /// </summary>
    public Result CanCancel()
    {
        if (Status == InvoiceStatus.Paid)
        {
            return Result.Failure(BillingErrors.CannotCancelPaidInvoice);
        }

        if (Status == InvoiceStatus.Cancelled)
        {
            return Result.Failure(BillingErrors.InvoiceCancelled);
        }

        if (Payments.Any(p => p.Status == PaymentStatus.Pending))
        {
            return Result.Failure(BillingErrors.CannotCancelInvoiceWithPendingPayment);
        }

        return Result.Success();
    }

    /// <summary>
    /// Thực hiện hủy hóa đơn
    /// </summary>
    public Result Cancel(string reason)
    {
        var check = CanCancel();
        if (check.IsFailure)
        {
            return check;
        }

        Status = InvoiceStatus.Cancelled;
        CancelReason = reason;
        CancelledAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Đánh dấu hóa đơn đã thanh toán thành công (BR-BIL-24)
    /// </summary>
    public void MarkAsPaid(DateTimeOffset paidAt)
    {
        PaidAmount = TotalAmount;
        PaidAt = paidAt;
        Status = InvoiceStatus.Paid;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cập nhật trạng thái quá hạn nếu đã qua due_date (BR-BIL-26)
    /// </summary>
    public bool CheckAndSetOverdue(DateOnly today)
    {
        if (Status == InvoiceStatus.Unpaid && DueDate < today)
        {
            Status = InvoiceStatus.Overdue;
            UpdatedAt = DateTimeOffset.UtcNow;
            return true;
        }

        return false;
    }
}
