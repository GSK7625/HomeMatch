using HomeMatch.Modules.Leasing.Domain.Enums;

namespace HomeMatch.Modules.Leasing.Domain.Entities;

public class Contract
{
    public long Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public long RoomId { get; set; }
    public long TenantId { get; set; }
    public long LandlordId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public long MonthlyRent { get; set; }
    public long DepositAmount { get; set; }
    public DepositStatus DepositStatus { get; set; } = DepositStatus.PENDING;
    public int BillingDueDay { get; set; } = 5;
    public string? ContractFileUrl { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.DRAFT;
    public string? TermsSnapshot { get; set; }
    public DateTimeOffset? TenantConfirmedAt { get; set; }
    public DateTimeOffset? LandlordConfirmedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public string? TerminationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public void UpdateDraftTerms(DateOnly startDate, DateOnly endDate, long monthlyRent, long depositAmount, int billingDueDay, string? termsSnapshot)
    {
        if (Status != ContractStatus.DRAFT)
        {
            throw new InvalidOperationException("Chỉ có thể chỉnh sửa điều khoản khi hợp đồng ở trạng thái BẢN NHÁP (DRAFT).");
        }

        if (endDate <= startDate)
        {
            throw new ArgumentException("Ngày kết thúc hợp đồng phải sau ngày bắt đầu.");
        }

        if (monthlyRent <= 0)
        {
            throw new ArgumentException("Giá thuê hàng tháng phải lớn hơn 0.");
        }

        if (depositAmount < 0)
        {
            throw new ArgumentException("Tiền cọc không được nhỏ hơn 0.");
        }

        if (billingDueDay < 1 || billingDueDay > 28)
        {
            throw new ArgumentException("Ngày chốt thanh toán hàng tháng phải từ ngày 1 đến 28.");
        }

        StartDate = startDate;
        EndDate = endDate;
        MonthlyRent = monthlyRent;
        DepositAmount = depositAmount;
        BillingDueDay = billingDueDay;
        TermsSnapshot = termsSnapshot;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SendToTenant(string termsSnapshot)
    {
        if (Status != ContractStatus.DRAFT)
        {
            throw new InvalidOperationException($"Chỉ có thể gửi hợp đồng cho người thuê khi đang ở trạng thái DRAFT. Trạng thái hiện tại: {Status}");
        }

        if (EndDate <= StartDate)
        {
            throw new InvalidOperationException("Thời hạn thuê không hợp lệ (Ngày kết thúc phải sau ngày bắt đầu).");
        }

        Status = ContractStatus.PENDING_TENANT_CONFIRMATION;
        TermsSnapshot = termsSnapshot;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TenantConfirm(long tenantUserId, string termsSnapshotToVerify)
    {
        if (TenantId != tenantUserId)
        {
            throw new UnauthorizedAccessException("Người dùng không có quyền xác nhận hợp đồng này.");
        }

        if (Status != ContractStatus.PENDING_TENANT_CONFIRMATION)
        {
            throw new InvalidOperationException($"Hợp đồng không ở trạng thái chờ người thuê xác nhận. Trạng thái hiện tại: {Status}");
        }

        // Chống sửa đổi âm thầm: So khớp phiên bản điều khoản mà người thuê nhìn thấy với bản snapshot
        if (!string.IsNullOrEmpty(TermsSnapshot) && TermsSnapshot != termsSnapshotToVerify)
        {
            throw new InvalidOperationException("Nội dung hợp đồng đã bị thay đổi kể từ khi gửi. Vui lòng tải lại và xem xét bản mới nhất.");
        }

        Status = ContractStatus.PENDING_LANDLORD_CONFIRMATION;
        TenantConfirmedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TenantReject(long tenantUserId, string reason)
    {
        if (TenantId != tenantUserId)
        {
            throw new UnauthorizedAccessException("Người dùng không có quyền từ chối hợp đồng này.");
        }

        if (Status != ContractStatus.PENDING_TENANT_CONFIRMATION)
        {
            throw new InvalidOperationException($"Hợp đồng không ở trạng thái chờ người thuê xác nhận để từ chối. Trạng thái hiện tại: {Status}");
        }

        Status = ContractStatus.REJECTED;
        RejectionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void LandlordFinalize(long landlordUserId)
    {
        if (LandlordId != landlordUserId)
        {
            throw new UnauthorizedAccessException("Người dùng không phải chủ nhà sở hữu hợp đồng này.");
        }

        if (Status != ContractStatus.PENDING_LANDLORD_CONFIRMATION)
        {
            throw new InvalidOperationException($"Hợp đồng phải được người thuê xác nhận trước khi chủ nhà kích hoạt. Trạng thái hiện tại: {Status}");
        }

        Status = ContractStatus.ACTIVE;
        LandlordConfirmedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(long userId, string reason)
    {
        if (LandlordId != userId && TenantId != userId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy hợp đồng này.");
        }

        if (Status == ContractStatus.ACTIVE || Status == ContractStatus.COMPLETED)
        {
            throw new InvalidOperationException("Không thể hủy hợp đồng đang có hiệu lực hoặc đã hoàn thành. Hãy dùng chức năng thanh lý/chấm dứt (Terminate).");
        }

        Status = ContractStatus.CANCELLED;
        CancellationReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Terminate(long landlordUserId, string reason)
    {
        if (LandlordId != landlordUserId)
        {
            throw new UnauthorizedAccessException("Chỉ chủ nhà mới có quyền chấm dứt/thanh lý hợp đồng.");
        }

        if (Status != ContractStatus.ACTIVE)
        {
            throw new InvalidOperationException("Chỉ có thể chấm dứt/thanh lý hợp đồng đang có hiệu lực (ACTIVE).");
        }

        Status = ContractStatus.TERMINATED;
        TerminationReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
