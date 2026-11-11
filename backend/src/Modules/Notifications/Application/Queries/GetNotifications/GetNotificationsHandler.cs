using HomeMatch.Modules.Notifications.Application.DTOs;
using HomeMatch.Modules.Notifications.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Notifications.Application.Queries.GetNotifications;

public sealed class GetNotificationsHandler
{
    private readonly INotificationRepository _repository;

    public GetNotificationsHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedNotificationsDto>> HandleAsync(
        GetNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount, unreadCount) = await _repository.GetPagedByUserAsync(
            query.UserId,
            query.IsRead,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(n => new NotificationDto(
            Id: n.Id,
            UserId: n.UserId,
            Title: n.Title,
            Content: n.Content,
            Type: n.Type,
            ReferenceType: n.ReferenceType,
            ReferenceId: n.ReferenceId,
            IsRead: n.IsRead,
            CreatedAt: n.CreatedAt)).ToList();

        return Result.Success(new PagedNotificationsDto(
            Items: dtos,
            UnreadCount: unreadCount,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount));
    }
}
