namespace HomeMatch.Modules.Identity.Application.Features.Profile.ChangePassword;

public record ChangePasswordCommand(
    long UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword,
    int? TokenSecurityVersion = null
);

public record ChangePasswordResult(string Message);
