namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Responses;

public record RegisterResponse(string Message);

public record ConfirmEmailResponse(string Message);

public record ResendConfirmationResponse(string Message);

public record IssuedAccessTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset ExpiresAt,
    DateTimeOffset SessionExpiresAt
);

public record ForgotPasswordResponse(string Message);

public record ResetPasswordResponse(string Message);
