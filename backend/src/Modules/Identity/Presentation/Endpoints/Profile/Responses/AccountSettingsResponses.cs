namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Profile.Responses;

public record NotificationSettingsResponse(
    bool NotifyPush,
    DateTimeOffset UpdatedAt
);

public record ChangePasswordResponse(
    string Message
);
