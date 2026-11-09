namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public record ResetPasswordRequest(
    long UserId,
    string Token,
    string NewPassword
);
