using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

public sealed record CreateContractRequest(
    long RoomId,
    long TenantId,
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    int BillingDueDay = 5,
    string? ContractCode = null,
    string? ContractFileUrl = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (RoomId <= 0)
        {
            errors.Add(new ErrorDetail("roomId", "Mã phòng cho thuê (roomId) phải lớn hơn 0."));
        }

        if (TenantId <= 0)
        {
            errors.Add(new ErrorDetail("tenantId", "Mã người thuê (tenantId) phải lớn hơn 0."));
        }

        if (StartDate == default)
        {
            errors.Add(new ErrorDetail("startDate", "Ngày bắt đầu thuê không hợp lệ."));
        }

        if (EndDate == default)
        {
            errors.Add(new ErrorDetail("endDate", "Ngày kết thúc thuê không hợp lệ."));
        }

        if (StartDate != default && EndDate != default && StartDate >= EndDate)
        {
            errors.Add(new ErrorDetail("dateRange", "Ngày bắt đầu thuê phải trước ngày kết thúc thuê."));
        }

        if (MonthlyRent <= 0)
        {
            errors.Add(new ErrorDetail("monthlyRent", "Tiền thuê hàng tháng phải lớn hơn 0 VNĐ."));
        }

        if (DepositAmount < 0)
        {
            errors.Add(new ErrorDetail("depositAmount", "Tiền đặt cọc không được là số âm."));
        }

        if (BillingDueDay < 1 || BillingDueDay > 28)
        {
            errors.Add(new ErrorDetail("billingDueDay", "Ngày thu tiền định kỳ hàng tháng phải nằm trong khoảng từ ngày 1 đến ngày 28."));
        }

        if (!string.IsNullOrWhiteSpace(ContractCode) && ContractCode.Length > 50)
        {
            errors.Add(new ErrorDetail("contractCode", "Mã hợp đồng tùy chỉnh không được vượt quá 50 ký tự."));
        }

        if (!string.IsNullOrWhiteSpace(ContractFileUrl) && ContractFileUrl.Length > 1000)
        {
            errors.Add(new ErrorDetail("contractFileUrl", "Đường dẫn file hợp đồng không được vượt quá 1000 ký tự."));
        }

        return errors;
    }
}

public sealed record SendForSignatureRequest(
    DateTimeOffset SignatureExpiresAt,
    DateTimeOffset? HandoverDueAt = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (SignatureExpiresAt <= DateTimeOffset.UtcNow)
        {
            errors.Add(new ErrorDetail("signatureExpiresAt", "Thời hạn ký hợp đồng phải lớn hơn thời điểm hiện tại."));
        }

        if (HandoverDueAt.HasValue && HandoverDueAt.Value < SignatureExpiresAt)
        {
            errors.Add(new ErrorDetail("handoverDueAt", "Hạn bàn giao phòng không thể trước thời hạn ký hợp đồng."));
        }

        return errors;
    }
}

public sealed record SignContractRequest(
    string DocumentHash,
    string OtpCode,
    string SignatureMethod = "OTP_EMAIL")
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(DocumentHash))
        {
            errors.Add(new ErrorDetail("documentHash", "Mã băm tài liệu ký (documentHash) không được để trống."));
        }
        else if (DocumentHash.Length > 256)
        {
            errors.Add(new ErrorDetail("documentHash", "Mã băm tài liệu ký không được vượt quá 256 ký tự."));
        }

        if (string.IsNullOrWhiteSpace(OtpCode))
        {
            errors.Add(new ErrorDetail("otpCode", "Mã xác thực OTP (otpCode) không được để trống."));
        }
        else if (OtpCode.Trim().Length < 4 || OtpCode.Trim().Length > 10)
        {
            errors.Add(new ErrorDetail("otpCode", "Mã OTP phải có độ dài hợp lệ từ 4 đến 10 ký tự."));
        }

        if (string.IsNullOrWhiteSpace(SignatureMethod))
        {
            errors.Add(new ErrorDetail("signatureMethod", "Phương thức ký không được để trống."));
        }
        else if (SignatureMethod.Length > 50)
        {
            errors.Add(new ErrorDetail("signatureMethod", "Tên phương thức ký không được vượt quá 50 ký tự."));
        }

        return errors;
    }
}

public sealed record RejectContractRequest(
    string Reason)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(Reason))
        {
            errors.Add(new ErrorDetail("reason", "Lý do từ chối ký hợp đồng không được để trống."));
        }
        else if (Reason.Length > 500)
        {
            errors.Add(new ErrorDetail("reason", "Lý do từ chối không được vượt quá 500 ký tự."));
        }

        return errors;
    }
}

public sealed record CancelContractRequest(
    string Reason)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(Reason))
        {
            errors.Add(new ErrorDetail("reason", "Lý do hủy hợp đồng không được để trống."));
        }
        else if (Reason.Length > 500)
        {
            errors.Add(new ErrorDetail("reason", "Lý do hủy không được vượt quá 500 ký tự."));
        }

        return errors;
    }
}

public sealed record ConfirmHandoverRequest(
    DateOnly? ActualOccupancyEndDate = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        return Array.Empty<ErrorDetail>();
    }
}

public sealed record TerminateContractRequest(
    string Reason)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(Reason))
        {
            errors.Add(new ErrorDetail("reason", "Lý do chấm dứt hợp đồng trước hạn không được để trống."));
        }
        else if (Reason.Length > 500)
        {
            errors.Add(new ErrorDetail("reason", "Lý do chấm dứt trước hạn không được vượt quá 500 ký tự."));
        }

        return errors;
    }
}

public sealed record RecordDepositTransactionRequest(
    string TransactionType,
    long Amount,
    string? Reason = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(TransactionType))
        {
            errors.Add(new ErrorDetail("transactionType", "Loại giao dịch cọc không được để trống."));
        }
        else
        {
            var normalized = TransactionType.Trim().ToUpperInvariant();
            if (normalized != "RECEIVED" && normalized != "REFUNDED" && normalized != "DEDUCTED")
            {
                errors.Add(new ErrorDetail("transactionType", "Loại giao dịch cọc phải là RECEIVED, REFUNDED hoặc DEDUCTED."));
            }

            if (normalized == "DEDUCTED" && string.IsNullOrWhiteSpace(Reason))
            {
                errors.Add(new ErrorDetail("reason", "Giao dịch khấu trừ tiền cọc bắt buộc phải cung cấp lý do (reason)."));
            }
        }

        if (Amount <= 0)
        {
            errors.Add(new ErrorDetail("amount", "Số tiền giao dịch cọc phải lớn hơn 0 VNĐ."));
        }

        if (!string.IsNullOrWhiteSpace(Reason) && Reason.Length > 500)
        {
            errors.Add(new ErrorDetail("reason", "Lý do giao dịch không được vượt quá 500 ký tự."));
        }

        return errors;
    }
}

public sealed record RecordDepositReceivedRequest(
    long Amount)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();
        if (Amount <= 0)
        {
            errors.Add(new ErrorDetail("amount", "Số tiền nhận cọc phải lớn hơn 0 VNĐ."));
        }
        return errors;
    }
}

public sealed record RecordDepositRefundRequest(
    long Amount,
    string? Reason = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();
        if (Amount <= 0)
        {
            errors.Add(new ErrorDetail("amount", "Số tiền hoàn cọc phải lớn hơn 0 VNĐ."));
        }
        if (!string.IsNullOrWhiteSpace(Reason) && Reason.Length > 500)
        {
            errors.Add(new ErrorDetail("reason", "Lý do hoàn cọc không được vượt quá 500 ký tự."));
        }
        return errors;
    }
}

public sealed record RecordDepositDeductionRequest(
    long Amount,
    string Reason)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();
        if (Amount <= 0)
        {
            errors.Add(new ErrorDetail("amount", "Số tiền khấu trừ cọc phải lớn hơn 0 VNĐ."));
        }
        if (string.IsNullOrWhiteSpace(Reason))
        {
            errors.Add(new ErrorDetail("reason", "Giao dịch khấu trừ tiền cọc bắt buộc phải cung cấp lý do (reason)."));
        }
        else if (Reason.Length > 500)
        {
            errors.Add(new ErrorDetail("reason", "Lý do khấu trừ cọc không được vượt quá 500 ký tự."));
        }
        return errors;
    }
}
