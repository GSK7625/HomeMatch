using HomeMatch.Modules.Notifications.Application.Interfaces;
using HomeMatch.Modules.Notifications.Domain.Entities;
using HomeMatch.Shared.Contracts.Notifications;

namespace HomeMatch.Modules.Notifications.Infrastructure.Services;

internal sealed class NotificationPublisher : INotificationPublisher
{
    private readonly INotificationRepository _repository;

    public NotificationPublisher(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task PublishAsync(
        long userId,
        string title,
        string content,
        string type,
        string? referenceType = null,
        long? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return;
        }

        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Type = type,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(notification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
