namespace HomeMatch.Modules.Identity.Application.Features.Auth.ConfirmEmail;

public record ConfirmEmailCommand(
    long UserId,
    string Token
);

public record ConfirmEmailResult(string Message);
