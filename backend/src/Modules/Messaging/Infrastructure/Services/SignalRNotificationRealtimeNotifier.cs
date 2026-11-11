using HomeMatch.Modules.Messaging.Presentation.Hubs;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace HomeMatch.Modules.Messaging.Infrastructure.Services;

internal sealed class SignalRNotificationRealtimeNotifier(IHubContext<ChatHub> hubContext) : INotificationRealtimeNotifier
{
    public async Task NotifyAsync(NotificationRealtimeItem notification, CancellationToken cancellationToken = default)
    {
        var userGroup = ChatHub.GetUserGroup(notification.UserId);
        await hubContext.Clients.Group(userGroup).SendAsync(
            "ReceiveNotification",
            new
            {
                id = notification.Id,
                userId = notification.UserId,
                title = notification.Title,
                content = notification.Content,
                type = notification.Type,
                referenceType = notification.ReferenceType,
                referenceId = notification.ReferenceId,
                createdAt = notification.CreatedAt.ToString("o"),
                isRead = false
            },
            cancellationToken);
    }
}
