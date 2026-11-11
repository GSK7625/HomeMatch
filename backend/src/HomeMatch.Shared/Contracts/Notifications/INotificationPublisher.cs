namespace HomeMatch.Shared.Contracts.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(
        long userId,
        string title,
        string content,
        string type,
        string? referenceType = null,
        long? referenceId = null,
        CancellationToken cancellationToken = default);
}
