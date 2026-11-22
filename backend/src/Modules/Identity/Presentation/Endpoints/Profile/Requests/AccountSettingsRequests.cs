namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Profile.Requests;

public record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);

public record UpdateNotificationSettingsRequest(
    bool NotifyPush
);
