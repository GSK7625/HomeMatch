namespace HomeMatch.Shared.Contracts.Notifications;

public sealed record NotificationRealtimeItem(
    long Id,
    long UserId,
    string Title,
    string Content,
    string Type,
    string? ReferenceType,
    long? ReferenceId,
    DateTimeOffset CreatedAt);

public interface INotificationRealtimeNotifier
{
    Task NotifyAsync(NotificationRealtimeItem notification, CancellationToken cancellationToken = default);
}
