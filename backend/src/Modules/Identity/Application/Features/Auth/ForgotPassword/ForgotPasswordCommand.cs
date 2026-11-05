namespace HomeMatch.Modules.Identity.Application.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(
    string Email
);

public record ForgotPasswordResult(
    string Message
);
