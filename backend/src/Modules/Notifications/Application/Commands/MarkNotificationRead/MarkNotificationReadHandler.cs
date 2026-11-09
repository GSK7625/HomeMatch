using HomeMatch.Modules.Notifications.Application.DTOs;
using HomeMatch.Modules.Notifications.Application.Interfaces;
using HomeMatch.Modules.Notifications.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Notifications.Application.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadHandler
{
    private readonly INotificationRepository _repository;

    public MarkNotificationReadHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<NotificationDto>> HandleAsync(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(command.NotificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure<NotificationDto>(NotificationErrors.NotFound);
        }

        if (notification.UserId != command.UserId)
        {
            return Result.Failure<NotificationDto>(NotificationErrors.Forbidden);
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new NotificationDto(
            Id: notification.Id,
            UserId: notification.UserId,
            Title: notification.Title,
            Content: notification.Content,
            Type: notification.Type,
            ReferenceType: notification.ReferenceType,
            ReferenceId: notification.ReferenceId,
            IsRead: notification.IsRead,
            CreatedAt: notification.CreatedAt));
    }
}
