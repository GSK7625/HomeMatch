namespace HomeMatch.Modules.Identity.Application.Features.Auth.ResendConfirmation;

public record ResendConfirmationCommand(
    string Email
);

public record ResendConfirmationResult(string Message);
