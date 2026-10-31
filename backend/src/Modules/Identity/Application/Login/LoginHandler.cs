using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Application.Login;

public sealed class LoginHandler(ILoginStore store)
{
    public Task<LoginResult> HandleAsync(LoginCommand command, string? previousRefreshSecret, CancellationToken cancellationToken)
    {
        // Verify existing credentials without applying today's password creation rules.
        if (!AccountInput.TryNormalizeEmail(command.Email, out _, out var normalized)
            || command.Password is null || command.Password.Length > 256
            || !AccountInput.TryCountScalars(command.Password, out var length) || length is < 1 or > 128)
            return Task.FromResult(new LoginResult(LoginOutcome.InvalidInput));

        return store.AuthenticateAndStartSessionAsync(new ValidatedLogin(normalized, command.Password), previousRefreshSecret, cancellationToken);
    }
}
