namespace HomeMatch.Modules.Notifications.Application.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(
    long NotificationId,
    long UserId);
