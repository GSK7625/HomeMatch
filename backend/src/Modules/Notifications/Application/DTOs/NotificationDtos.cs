namespace HomeMatch.Modules.Notifications.Application.DTOs;

public record NotificationDto(
    long Id,
    long UserId,
    string Title,
    string Content,
    string Type,
    string? ReferenceType,
    long? ReferenceId,
    bool IsRead,
    DateTimeOffset CreatedAt);

public record PagedNotificationsDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount,
    int Page,
    int PageSize,
    int TotalCount);
