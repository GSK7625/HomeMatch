using HomeMatch.Modules.Notifications.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Notifications.Application.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadHandler
{
    private readonly INotificationRepository _repository;

    public MarkAllNotificationsReadHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> HandleAsync(
        MarkAllNotificationsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var count = await _repository.MarkAllAsReadAsync(command.UserId, cancellationToken);
        return Result.Success(count);
    }
}
