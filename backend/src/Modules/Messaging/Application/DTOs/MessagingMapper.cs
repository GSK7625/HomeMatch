using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Messaging.Application.DTOs;

internal static class MessagingMapper
{
    public static ConversationDto ToConversationDto(
        Conversation conversation,
        long currentUserId,
        ListingContractInfo? listing,
        IdentityUserInfo? initiator,
        IdentityUserInfo? recipient,
        Message? lastMessage,
        int unreadCount)
    {
        var isInitiator = conversation.InitiatorId == currentUserId;
        var partner = isInitiator ? recipient : initiator;
        var partnerId = isInitiator ? conversation.RecipientId : conversation.InitiatorId;

        return new ConversationDto(
            Id: conversation.Id,
            ConversationType: conversation.ConversationType,
            ListingId: conversation.ListingId,
            ListingTitle: listing?.Title,
            ListingPrice: listing?.Price,
            ListingPrimaryImageUrl: listing?.PrimaryImageUrl,
            InitiatorId: conversation.InitiatorId,
            InitiatorName: initiator?.FullName ?? $"Người dùng #{conversation.InitiatorId}",
            RecipientId: conversation.RecipientId,
            RecipientName: recipient?.FullName ?? $"Người dùng #{conversation.RecipientId}",
            PartnerId: partnerId,
            PartnerName: partner?.FullName ?? $"Người dùng #{partnerId}",
            PartnerPhone: partner?.PhoneNumber,
            LastMessageContent: lastMessage?.Content,
            LastMessageSenderId: lastMessage?.SenderId,
            UnreadCount: unreadCount,
            LastMessageAt: conversation.LastMessageAt,
            CreatedAt: conversation.CreatedAt);
    }

    public static MessageDto ToMessageDto(Message message, IdentityUserInfo? sender)
    {
        return new MessageDto(
            Id: message.Id,
            ConversationId: message.ConversationId,
            SenderId: message.SenderId,
            SenderName: sender?.FullName ?? $"Người dùng #{message.SenderId}",
            MessageType: message.MessageType,
            Content: message.Content,
            IsRead: message.IsRead,
            CreatedAt: message.CreatedAt);
    }
}
