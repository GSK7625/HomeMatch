using HomeMatch.Modules.Notifications.Application.Interfaces;
using HomeMatch.Modules.Notifications.Domain.Entities;
using HomeMatch.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Notifications.Infrastructure.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetPagedByUserAsync(
        long userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        var unreadCount = await baseQuery
            .CountAsync(n => !n.IsRead, cancellationToken);

        var filteredQuery = isRead.HasValue
            ? baseQuery.Where(n => n.IsRead == isRead.Value)
            : baseQuery;

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var items = await filteredQuery
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, unreadCount);
    }

    public async Task<int> MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        var unreadList = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadList.Count == 0)
        {
            return 0;
        }

        foreach (var item in unreadList)
        {
            item.IsRead = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return unreadList.Count;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
