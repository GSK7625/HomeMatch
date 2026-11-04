namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public record ConfirmEmailRequest(
    long UserId,
    string Token
);
