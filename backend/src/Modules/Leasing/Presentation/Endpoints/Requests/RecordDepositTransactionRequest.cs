using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

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
