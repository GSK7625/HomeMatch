using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

public sealed record UpdateDraftContractRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    int BillingDueDay,
    string? DocumentSnapshot = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (StartDate == default || EndDate == default || EndDate <= StartDate)
        {
            errors.Add(new ErrorDetail("dateRange", "Ngày kết thúc thuê phải sau ngày bắt đầu thuê."));
        }

        if (MonthlyRent <= 0)
        {
            errors.Add(new ErrorDetail("monthlyRent", "Tiền thuê hàng tháng phải lớn hơn 0 VNĐ."));
        }

        if (DepositAmount < 0)
        {
            errors.Add(new ErrorDetail("depositAmount", "Tiền đặt cọc không được nhỏ hơn 0 VNĐ."));
        }

        if (BillingDueDay is < 1 or > 28)
        {
            errors.Add(new ErrorDetail("billingDueDay", "Ngày đóng tiền hàng tháng phải từ ngày 1 đến ngày 28."));
        }

        return errors;
    }
}
