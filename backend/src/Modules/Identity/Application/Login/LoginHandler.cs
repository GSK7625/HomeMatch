using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Application.Login;

public sealed class LoginHandler(ILoginStore store)
{
    public Task<LoginResult> HandleAsync(LoginCommand command, string? previousRefreshSecret, CancellationToken cancellationToken)
    {
        // Existing credentials are verified, not re-evaluated against today's creation policy/blocklist.
        if (!AccountInput.TryNormalizeEmail(command.Email, out _, out var normalized)
            || command.RememberMe is null || command.Password is null || command.Password.Length > 256
            || !AccountInput.TryCountScalars(command.Password, out var length) || length is < 1 or > 128)
            return Task.FromResult(new LoginResult(LoginOutcome.InvalidInput));

        return store.AuthenticateAndStartSessionAsync(new ValidatedLogin(normalized, command.Password,
            command.RememberMe.Value), previousRefreshSecret, cancellationToken);
    }
}
