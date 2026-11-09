using HomeMatch.Modules.Notifications.Domain.Entities;

namespace HomeMatch.Modules.Notifications.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetPagedByUserAsync(
        long userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
