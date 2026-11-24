using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Messaging.Presentation.Endpoints.Requests;

public sealed record UpdateLeadRequest(
    string? Status = null,
    string? Notes = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (Status is not null)
        {
            var normalized = Status.Trim().ToUpperInvariant();
            if (!LeadStatus.All.Contains(normalized))
            {
                errors.Add(new ErrorDetail(
                    "status",
                    "Trạng thái không hợp lệ. Chỉ chấp nhận: NEW, CONTACTED, VIEWING_SCHEDULED, CONTRACT_SIGNED, LOST."));
            }
        }

        if (Notes is not null && Notes.Length > 1000)
        {
            errors.Add(new ErrorDetail("notes", "Ghi chú không được vượt quá 1000 ký tự."));
        }

        return errors;
    }
}
