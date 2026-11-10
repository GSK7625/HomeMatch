using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Domain.Entities;

/// <summary>
/// Thực thể Hợp đồng thuê phòng pháp lý giữa Bên Cho Thuê và Bên Thuê
/// </summary>
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
    public string DepositStatus { get; set; } = Enums.DepositStatus.Pending;
    public int BillingDueDay { get; set; } = 5;
    public string? ContractFileUrl { get; set; }
    public string? DocumentSnapshot { get; set; }
    public int DocumentVersion { get; set; } = 1;
    public DateTimeOffset? DocumentGeneratedAt { get; set; }
    public string Status { get; set; } = ContractStatus.Draft;

    // Chữ ký điện tử & Xác thực trực tuyến (OTP / Email)
    public DateTimeOffset? SignatureExpiresAt { get; set; }
    public DateTimeOffset? LandlordSignedAt { get; set; }
    public DateTimeOffset? TenantSignedAt { get; set; }
    public string? SignedDocumentHash { get; set; }

    // Quản lý Bàn giao phòng & Thời gian chiếm dụng thực tế
    public DateTimeOffset? HandoverDueAt { get; set; }
    public DateTimeOffset? HandoverConfirmedAt { get; set; }
    public long? HandoverConfirmedBy { get; set; }
    public DateOnly? ActualOccupancyEndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Lịch sử giao dịch tiền cọc độc lập
    public ICollection<DepositTransaction> DepositTransactions { get; set; } = new List<DepositTransaction>();

    /// <summary>
    /// Factory tạo hợp đồng mới ở trạng thái DRAFT
    /// </summary>
    public static Result<Contract> Create(
        string contractCode,
        long roomId,
        long tenantId,
        long landlordId,
        DateOnly startDate,
        DateOnly endDate,
        long monthlyRent,
        long depositAmount,
        int billingDueDay = 5,
        string? contractFileUrl = null)
    {
        if (string.IsNullOrWhiteSpace(contractCode))
        {
            return Result.Failure<Contract>(new Error("Leasing.InvalidCode", "Mã hợp đồng không được để trống."));
        }

        if (roomId <= 0 || tenantId <= 0 || landlordId <= 0)
        {
            return Result.Failure<Contract>(new Error("Leasing.InvalidIdentifiers", "ID phòng, người thuê hoặc chủ nhà không hợp lệ."));
        }

        if (tenantId == landlordId)
        {
            return Result.Failure<Contract>(LeasingErrors.SelfLeasingNotAllowed);
        }

        if (endDate <= startDate)
        {
            return Result.Failure<Contract>(LeasingErrors.InvalidDateRange);
        }

        if (monthlyRent <= 0)
        {
            return Result.Failure<Contract>(LeasingErrors.InvalidMonthlyRent);
        }

        if (depositAmount < 0)
        {
            return Result.Failure<Contract>(LeasingErrors.InvalidDepositAmount);
        }

        if (billingDueDay < 1 || billingDueDay > 28)
        {
            return Result.Failure<Contract>(LeasingErrors.InvalidBillingDueDay);
        }

        var now = DateTimeOffset.UtcNow;
        var contract = new Contract
        {
            ContractCode = contractCode.Trim(),
            RoomId = roomId,
            TenantId = tenantId,
            LandlordId = landlordId,
            StartDate = startDate,
            EndDate = endDate,
            MonthlyRent = monthlyRent,
            DepositAmount = depositAmount,
            DepositStatus = Enums.DepositStatus.Pending,
            BillingDueDay = billingDueDay,
            ContractFileUrl = contractFileUrl?.Trim(),
            Status = ContractStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        return Result.Success(contract);
    }

    /// <summary>
    /// Chỉnh sửa hợp đồng DRAFT (BR-07, BR-08)
    /// </summary>
    public Result UpdateDraft(
        DateOnly startDate,
        DateOnly endDate,
        long monthlyRent,
        long depositAmount,
        int billingDueDay,
        string? contractFileUrl)
    {
        if (Status != ContractStatus.Draft)
        {
            return Result.Failure(LeasingErrors.CannotModifyNonDraft);
        }

        if (endDate <= startDate)
        {
            return Result.Failure(LeasingErrors.InvalidDateRange);
        }

        if (monthlyRent <= 0)
        {
            return Result.Failure(LeasingErrors.InvalidMonthlyRent);
        }

        if (depositAmount < 0)
        {
            return Result.Failure(LeasingErrors.InvalidDepositAmount);
        }

        if (billingDueDay < 1 || billingDueDay > 28)
        {
            return Result.Failure(LeasingErrors.InvalidBillingDueDay);
        }

        StartDate = startDate;
        EndDate = endDate;
        MonthlyRent = monthlyRent;
        DepositAmount = depositAmount;
        BillingDueDay = billingDueDay;
        ContractFileUrl = contractFileUrl?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Cập nhật thông tin bản nháp hợp đồng khi còn ở DRAFT
    /// </summary>
    public Result UpdateDraft(
        DateOnly startDate,
        DateOnly endDate,
        long monthlyRent,
        long depositAmount,
        int billingDueDay)
    {
        if (Status != ContractStatus.Draft)
        {
            return Result.Failure(LeasingErrors.CannotModifyNonDraft);
        }

        if (endDate <= startDate)
        {
            return Result.Failure(LeasingErrors.InvalidDateRange);
        }

        if (monthlyRent <= 0)
        {
            return Result.Failure(LeasingErrors.InvalidMonthlyRent);
        }

        if (depositAmount < 0)
        {
            return Result.Failure(LeasingErrors.InvalidDepositAmount);
        }

        if (billingDueDay < 1 || billingDueDay > 28)
        {
            return Result.Failure(LeasingErrors.InvalidBillingDueDay);
        }

        StartDate = startDate;
        EndDate = endDate;
        MonthlyRent = monthlyRent;
        DepositAmount = depositAmount;
        BillingDueDay = billingDueDay;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Nộp file PDF chính thức, lưu snapshot thông tin và chuyển sang PENDING_SIGNATURE
    /// </summary>
    public Result SubmitForSignature(
        string contractFileUrl,
        string documentHash,
        string? documentSnapshot,
        DateTimeOffset signatureExpiresAt,
        DateTimeOffset? handoverDueAt = null)
    {
        if (Status != ContractStatus.Draft)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.PendingSignature));
        }

        if (string.IsNullOrWhiteSpace(contractFileUrl))
        {
            return Result.Failure(new Error("Leasing.ContractFileRequired", "Đường dẫn tài liệu hợp đồng không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(documentHash))
        {
            return Result.Failure(LeasingErrors.SignatureEvidenceRequired);
        }

        var now = DateTimeOffset.UtcNow;
        if (signatureExpiresAt <= now)
        {
            return Result.Failure(new Error("Leasing.InvalidSignatureDeadline", "Hạn ký hợp đồng trực tuyến phải nằm ở tương lai."));
        }

        ContractFileUrl = contractFileUrl;
        SignedDocumentHash = documentHash;
        DocumentSnapshot = documentSnapshot;
        DocumentGeneratedAt = now;
        Status = ContractStatus.PendingSignature;
        SignatureExpiresAt = signatureExpiresAt.ToUniversalTime();
        HandoverDueAt = (handoverDueAt ?? new DateTimeOffset(EndDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(7))).ToUniversalTime();
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Gửi hợp đồng cho hai bên ký (DRAFT -> PENDING_SIGNATURE, giữ lịch phòng tạm thời)
    /// </summary>
    public Result SendForSignature(DateTimeOffset signatureExpiresAt, DateTimeOffset? handoverDueAt = null)
    {
        if (Status != ContractStatus.Draft)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.PendingSignature));
        }

        var now = DateTimeOffset.UtcNow;
        if (signatureExpiresAt <= now)
        {
            return Result.Failure(new Error("Leasing.InvalidSignatureDeadline", "Hạn ký hợp đồng trực tuyến phải nằm ở tương lai."));
        }

        Status = ContractStatus.PendingSignature;
        SignatureExpiresAt = signatureExpiresAt.ToUniversalTime();
        HandoverDueAt = (handoverDueAt ?? new DateTimeOffset(EndDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(7))).ToUniversalTime();
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Landlord ký xác nhận qua OTP/email
    /// </summary>
    public Result SignByLandlord(string documentHash, DateTimeOffset signedAt)
    {
        if (Status != ContractStatus.PendingSignature)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.Signed));
        }

        if (SignatureExpiresAt.HasValue && signedAt > SignatureExpiresAt.Value)
        {
            return Result.Failure(LeasingErrors.SignatureExpired);
        }

        if (LandlordSignedAt.HasValue)
        {
            return Result.Failure(LeasingErrors.AlreadySignedByParty);
        }

        if (string.IsNullOrWhiteSpace(documentHash))
        {
            return Result.Failure(LeasingErrors.SignatureEvidenceRequired);
        }

        if (!string.IsNullOrEmpty(SignedDocumentHash) && !string.Equals(SignedDocumentHash, documentHash.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(LeasingErrors.DocumentHashMismatch);
        }

        LandlordSignedAt = signedAt.ToUniversalTime();
        SignedDocumentHash = documentHash.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        // Nếu cả hai bên đều đã ký thành công -> Hợp đồng chuyển sang SIGNED (BR-09)
        if (TenantSignedAt.HasValue)
        {
            Status = ContractStatus.Signed;
        }

        return Result.Success();
    }

    /// <summary>
    /// Tenant ký xác nhận qua OTP/email
    /// </summary>
    public Result SignByTenant(string documentHash, DateTimeOffset signedAt)
    {
        if (Status != ContractStatus.PendingSignature)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.Signed));
        }

        if (SignatureExpiresAt.HasValue && signedAt > SignatureExpiresAt.Value)
        {
            return Result.Failure(LeasingErrors.SignatureExpired);
        }

        if (TenantSignedAt.HasValue)
        {
            return Result.Failure(LeasingErrors.AlreadySignedByParty);
        }

        if (string.IsNullOrWhiteSpace(documentHash))
        {
            return Result.Failure(LeasingErrors.SignatureEvidenceRequired);
        }

        if (!string.IsNullOrEmpty(SignedDocumentHash) && !string.Equals(SignedDocumentHash, documentHash.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(LeasingErrors.DocumentHashMismatch);
        }

        TenantSignedAt = signedAt.ToUniversalTime();
        SignedDocumentHash = documentHash.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        // Nếu cả hai bên đều đã ký thành công -> Hợp đồng chuyển sang SIGNED (BR-09)
        if (LandlordSignedAt.HasValue)
        {
            Status = ContractStatus.Signed;
        }

        return Result.Success();
    }

    /// <summary>
    /// Một bên từ chối ký -> Hợp đồng chuyển REJECTED, giải phóng giữ phòng (BR-12)
    /// </summary>
    public Result Reject(string reason)
    {
        if (Status != ContractStatus.PendingSignature)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.Rejected));
        }

        Status = ContractStatus.Rejected;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Hết thời hạn ký mà chưa đủ chữ ký -> Hợp đồng chuyển SIGNATURE_EXPIRED, giải phóng giữ phòng (BR-13)
    /// </summary>
    public Result ExpireSignature(DateTimeOffset now)
    {
        if (Status != ContractStatus.PendingSignature)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.SignatureExpired));
        }

        if (SignatureExpiresAt.HasValue && now < SignatureExpiresAt.Value)
        {
            return Result.Failure(new Error("Leasing.SignatureNotExpiredYet", "Thời hạn ký hợp đồng chưa kết thúc."));
        }

        Status = ContractStatus.SignatureExpired;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Hủy hợp đồng trước khi ký hoàn tất
    /// </summary>
    public Result Cancel(string reason)
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PendingSignature)
        {
            return Result.Failure(LeasingErrors.CannotCancelActiveContract);
        }

        Status = ContractStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Kích hoạt hợp đồng khi đến ngày bắt đầu thuê (SIGNED -> ACTIVE) (BR-16)
    /// </summary>
    public Result Activate(DateOnly today)
    {
        if (Status != ContractStatus.Signed)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.Active));
        }

        if (today < StartDate)
        {
            return Result.Failure(new Error("Leasing.StartDateNotReached", $"Chưa đến ngày bắt đầu thuê ({StartDate:yyyy-MM-dd})."));
        }

        Status = ContractStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Đến ngày kết thúc thuê -> chuyển sang EXPIRED_PENDING_HANDOVER, không tự ý giải phóng phòng (BR-23)
    /// </summary>
    public Result MarkExpiredPendingHandover(DateOnly today)
    {
        if (Status != ContractStatus.Active)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.ExpiredPendingHandover));
        }

        if (today <= EndDate)
        {
            return Result.Failure(new Error("Leasing.EndDateNotReached", $"Hợp đồng chưa hết hạn thuê (ngày kết thúc: {EndDate:yyyy-MM-dd})."));
        }

        Status = ContractStatus.ExpiredPendingHandover;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Quá hạn bàn giao phòng -> chuyển sang HANDOVER_OVERDUE (BR-24)
    /// </summary>
    public Result MarkHandoverOverdue(DateOnly today)
    {
        if (Status != ContractStatus.ExpiredPendingHandover)
        {
            return Result.Failure(LeasingErrors.InvalidStatusTransition(Status, ContractStatus.HandoverOverdue));
        }

        if (HandoverDueAt.HasValue && today <= DateOnly.FromDateTime(HandoverDueAt.Value.Date))
        {
            return Result.Failure(new Error("Leasing.HandoverNotOverdueYet", "Chưa quá hạn bàn giao phòng."));
        }

        Status = ContractStatus.HandoverOverdue;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Chủ nhà xác nhận bàn giao phòng thành công -> COMPLETED (hoặc giữ TERMINATED nếu chấm dứt trước hạn), giải phóng quyền sử dụng phòng (BR-25, BR-30)
    /// </summary>
    public Result ConfirmHandover(long confirmedByLandlordId, DateTimeOffset confirmedAt, DateOnly actualOccupancyEndDate)
    {
        if (Status != ContractStatus.ExpiredPendingHandover &&
            Status != ContractStatus.HandoverOverdue &&
            Status != ContractStatus.Terminated)
        {
            return Result.Failure(LeasingErrors.HandoverNotPermitted);
        }

        if (HandoverConfirmedAt.HasValue)
        {
            return Result.Failure(LeasingErrors.HandoverAlreadyConfirmed);
        }

        if (confirmedByLandlordId != LandlordId)
        {
            return Result.Failure(LeasingErrors.OnlyLandlordCanConfirmHandover);
        }

        if (actualOccupancyEndDate < StartDate)
        {
            return Result.Failure(LeasingErrors.InvalidHandoverDate);
        }

        HandoverConfirmedBy = confirmedByLandlordId;
        HandoverConfirmedAt = confirmedAt.ToUniversalTime();
        ActualOccupancyEndDate = actualOccupancyEndDate;

        // Nếu là hợp đồng hết hạn bình thường -> COMPLETED. Nếu là TERMINATED -> Giữ nguyên TERMINATED để bảo lưu lịch sử chấm dứt sớm
        if (Status != ContractStatus.Terminated)
        {
            Status = ContractStatus.Completed;
        }

        UpdateDepositStatus();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Chấm dứt hợp đồng trước hạn (Mục 7.2) -> Vẫn phải giữ nghĩa vụ bàn giao phòng
    /// </summary>
    public Result TerminateEarly(string reason, DateTimeOffset terminatedAt)
    {
        if (Status != ContractStatus.Active)
        {
            return Result.Failure(LeasingErrors.CannotTerminateNonActiveContract);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(new Error("Leasing.TerminationReasonRequired", "Bắt buộc phải cung cấp lý do chấm dứt hợp đồng trước hạn."));
        }

        Status = ContractStatus.Terminated;
        UpdatedAt = terminatedAt.ToUniversalTime();

        return Result.Success();
    }

    // ==========================================================================================
    // QUẢN LÝ TIỀN CỌC & SỐ DƯ (DEPOSIT INVARIANTS)
    // ==========================================================================================

    public long GetTotalReceived() =>
        DepositTransactions.Where(t => t.TransactionType == DepositTransactionType.Received).Sum(t => t.Amount);

    public long GetTotalRefunded() =>
        DepositTransactions.Where(t => t.TransactionType == DepositTransactionType.Refunded).Sum(t => t.Amount);

    public long GetTotalDeducted() =>
        DepositTransactions.Where(t => t.TransactionType == DepositTransactionType.Deducted).Sum(t => t.Amount);

    public long GetCurrentDepositBalance() =>
        GetTotalReceived() - GetTotalRefunded() - GetTotalDeducted();

    /// <summary>
    /// Kiểm tra và thêm giao dịch tiền cọc, bảo vệ toàn vẹn số dư (BR-19, BR-20)
    /// </summary>
    public Result<DepositTransaction> AddDepositTransaction(string transactionType, long amount, string? reason, long recordedBy)
    {
        if (amount <= 0)
        {
            return Result.Failure<DepositTransaction>(LeasingErrors.InvalidDepositTransactionAmount);
        }

        if (transactionType != DepositTransactionType.Received &&
            transactionType != DepositTransactionType.Refunded &&
            transactionType != DepositTransactionType.Deducted)
        {
            return Result.Failure<DepositTransaction>(LeasingErrors.InvalidDepositTransactionType);
        }

        if (transactionType == DepositTransactionType.Deducted && string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<DepositTransaction>(LeasingErrors.DeductionReasonRequired);
        }

        if (transactionType == DepositTransactionType.Received)
        {
            if (Status != ContractStatus.Signed && Status != ContractStatus.Active && Status != ContractStatus.PendingSignature)
            {
                return Result.Failure<DepositTransaction>(LeasingErrors.DepositNotPermittedInStatus(Status, transactionType));
            }

            var totalAfter = GetTotalReceived() + amount;
            if (totalAfter > DepositAmount)
            {
                return Result.Failure<DepositTransaction>(LeasingErrors.DepositExceedsAgreed);
            }
        }
        else
        {
            // REFUNDED hoặc DEDUCTED không được vượt quá số dư hiện có (BR-20)
            if (Status != ContractStatus.Active &&
                Status != ContractStatus.ExpiredPendingHandover &&
                Status != ContractStatus.HandoverOverdue &&
                Status != ContractStatus.Terminated &&
                Status != ContractStatus.Completed)
            {
                return Result.Failure<DepositTransaction>(LeasingErrors.DepositNotPermittedInStatus(Status, transactionType));
            }

            var currentBalance = GetCurrentDepositBalance();
            if (amount > currentBalance)
            {
                return Result.Failure<DepositTransaction>(LeasingErrors.InsufficientDepositBalance);
            }
        }

        var txResult = DepositTransaction.Create(Id, transactionType, amount, reason, recordedBy);
        if (txResult.IsFailure)
        {
            return txResult;
        }

        var tx = txResult.Value;
        DepositTransactions.Add(tx);
        UpdateDepositStatus();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(tx);
    }

    public void UpdateDepositStatus()
    {
        var received = GetTotalReceived();
        var deducted = GetTotalDeducted();
        var balance = GetCurrentDepositBalance();

        if (received == 0)
        {
            DepositStatus = Enums.DepositStatus.Pending;
        }
        else if (balance == 0 && (Status == ContractStatus.Completed || Status == ContractStatus.Terminated))
        {
            DepositStatus = (deducted == received) ? Enums.DepositStatus.Forfeited : Enums.DepositStatus.Refunded;
        }
        else
        {
            DepositStatus = Enums.DepositStatus.Paid;
        }
    }
}
