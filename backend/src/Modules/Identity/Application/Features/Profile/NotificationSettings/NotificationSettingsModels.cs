namespace HomeMatch.Modules.Identity.Application.Features.Profile.NotificationSettings;

public record GetNotificationSettingsQuery(
    long UserId,
    int? TokenSecurityVersion = null
);

public record UpdateNotificationSettingsCommand(
    long UserId,
    bool NotifyPush,
    int? TokenSecurityVersion = null
);

public record NotificationSettingsResult(
    bool NotifyPush,
    DateTimeOffset UpdatedAt
);
