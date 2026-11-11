using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HomeMatch.Modules.Messaging.Infrastructure.Services;

internal sealed class SignalRChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatRealtimeNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyMessageSentAsync(
        long initiatorId,
        long recipientId,
        MessageDto message,
        CancellationToken cancellationToken = default)
    {
        var userGroups = new[]
        {
            ChatHub.GetUserGroup(initiatorId),
            ChatHub.GetUserGroup(recipientId)
        };

        // Đẩy sự kiện tin nhắn mới tới cả 2 thành viên (dù đang mở khung chat hay đang ở trang danh sách)
        await _hubContext.Clients.Groups(userGroups).SendAsync(
            "ReceiveMessage",
            message,
            cancellationToken);

        await _hubContext.Clients.Groups(userGroups).SendAsync(
            "ConversationUpdated",
            new
            {
                conversationId = message.ConversationId,
                lastMessageContent = message.Content,
                lastMessageSenderId = message.SenderId,
                lastMessageAt = message.CreatedAt
            },
            cancellationToken);
    }

    public async Task NotifyMessagesReadAsync(
        long conversationId,
        long readerUserId,
        long partnerUserId,
        int markedReadCount,
        CancellationToken cancellationToken = default)
    {
        if (markedReadCount <= 0)
        {
            return;
        }

        var payload = new
        {
            conversationId,
            readerUserId,
            markedReadCount,
            readAt = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.Groups(
            ChatHub.GetConversationGroup(conversationId),
            ChatHub.GetUserGroup(partnerUserId))
            .SendAsync("MessagesRead", payload, cancellationToken);
    }
}
