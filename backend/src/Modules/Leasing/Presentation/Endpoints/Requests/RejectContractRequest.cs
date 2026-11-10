using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

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
