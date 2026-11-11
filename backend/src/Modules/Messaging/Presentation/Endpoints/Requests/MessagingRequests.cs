using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Messaging.Presentation.Endpoints.Requests;

public sealed record StartConversationRequest(
    long ListingId,
    long? RecipientUserId = null,
    string? InitialMessage = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (ListingId <= 0)
        {
            errors.Add(new ErrorDetail("listingId", "Mã tin đăng phòng cho thuê (listingId) phải lớn hơn 0."));
        }

        if (InitialMessage is not null && InitialMessage.Length > 4000)
        {
            errors.Add(new ErrorDetail("initialMessage", "Tin nhắn mở đầu không được vượt quá 4000 ký tự."));
        }

        return errors;
    }
}

public sealed record SendMessageRequest(
    string Content,
    string MessageType = MessageType.Text)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(Content))
        {
            errors.Add(new ErrorDetail("content", "Nội dung tin nhắn không được để trống."));
        }
        else if (Content.Length > 4000)
        {
            errors.Add(new ErrorDetail("content", "Nội dung tin nhắn không được vượt quá 4000 ký tự."));
        }

        return errors;
    }
}

public sealed record SaveMessageDraftRequest(
    string Content,
    string Source = MessageDraftSource.Manual)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(Content))
        {
            errors.Add(new ErrorDetail("content", "Nội dung bản nháp không được để trống."));
        }
        else if (Content.Length > 4000)
        {
            errors.Add(new ErrorDetail("content", "Nội dung bản nháp không được vượt quá 4000 ký tự."));
        }

        return errors;
    }
}
