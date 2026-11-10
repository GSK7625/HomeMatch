using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

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
