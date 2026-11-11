namespace HomeMatch.Modules.Notifications.Application.Queries.GetNotifications;

public record GetNotificationsQuery(
    long UserId,
    bool? IsRead = null,
    int Page = 1,
    int PageSize = 20);
