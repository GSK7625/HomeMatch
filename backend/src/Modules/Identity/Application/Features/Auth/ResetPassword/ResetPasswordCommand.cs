namespace HomeMatch.Modules.Identity.Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(
    long UserId,
    string Token,
    string NewPassword
);

public record ResetPasswordResult(
    string Message
);
