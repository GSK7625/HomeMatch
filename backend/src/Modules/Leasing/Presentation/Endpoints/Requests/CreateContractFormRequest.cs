using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

public sealed class CreateContractFormRequest
{
    public long RoomId { get; set; }
    public long TenantId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public long MonthlyRent { get; set; }
    public long DepositAmount { get; set; }
    public int BillingDueDay { get; set; } = 5;
    public string? ContractCode { get; set; }
    public string? DocumentSnapshot { get; set; }
    public DateTimeOffset? SignatureExpiresAt { get; set; }
    public DateTimeOffset? HandoverDueAt { get; set; }

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
            errors.Add(new ErrorDetail("depositAmount", "Tiền cọc không được nhỏ hơn 0 VNĐ."));
        }

        if (BillingDueDay is < 1 or > 28)
        {
            errors.Add(new ErrorDetail("billingDueDay", "Ngày đóng tiền nhà hàng tháng phải từ ngày 1 đến ngày 28."));
        }

        if (SignatureExpiresAt.HasValue && SignatureExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            errors.Add(new ErrorDetail("signatureExpiresAt", "Hạn ký hợp đồng phải ở trong tương lai."));
        }

        if (HandoverDueAt.HasValue && HandoverDueAt.Value <= DateTimeOffset.UtcNow)
        {
            errors.Add(new ErrorDetail("handoverDueAt", "Hạn bàn giao phòng phải ở trong tương lai."));
        }

        return errors;
    }
}
