using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Messaging.Domain.Constants;

public static class MessagingErrors
{
    public static readonly Error AuthenticationRequired = new(
        "Messaging.AuthenticationRequired",
        "Chưa xác thực danh tính người dùng.");

    public static readonly Error ConversationNotFound = new(
        "Messaging.ConversationNotFound",
        "Không tìm thấy cuộc hội thoại yêu cầu.");

    public static readonly Error ListingNotFound = new(
        "Messaging.ListingNotFound",
        "Không tìm thấy tin đăng phòng cho thuê hoặc tin đăng hiện không khả dụng.");

    public static readonly Error Forbidden = new(
        "Messaging.Forbidden",
        "Bạn không phải là thành viên tham gia cuộc hội thoại này.");

    public static readonly Error SelfConversationNotAllowed = new(
        "Messaging.SelfConversationNotAllowed",
        "Không thể tự tạo cuộc hội thoại với chính mình.");

    public static readonly Error InvalidListingParticipant = new(
        "Messaging.InvalidListingParticipant",
        "Trong hội thoại thuê phòng (LISTING), bắt buộc một trong hai người tham gia phải là chủ sở hữu của tin đăng.");

    public static readonly Error EmptyMessageContent = new(
        "Messaging.EmptyMessageContent",
        "Nội dung tin nhắn không được để trống.");

    public static readonly Error InvalidMessageType = new(
        "Messaging.InvalidMessageType",
        "Loại tin nhắn không hợp lệ (chỉ hỗ trợ TEXT hoặc IMAGE).");
}
