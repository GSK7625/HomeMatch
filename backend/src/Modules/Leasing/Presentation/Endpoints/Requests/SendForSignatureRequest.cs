using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

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
