using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

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
