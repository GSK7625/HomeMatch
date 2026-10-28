using HomeMatch.Modules.Identity.Application.Authentication;

namespace HomeMatch.Modules.Identity.Application.Login;

public sealed record LoginCommand(string? Email, string? Password, bool? RememberMe)
{
    public override string ToString() => "LoginCommand { [redacted] }";
}

public sealed record ValidatedLogin(string NormalizedEmail, string Password, bool RememberMe)
{
    public override string ToString() => "ValidatedLogin { [redacted] }";
}

public enum LoginOutcome { Succeeded, InvalidInput, InvalidCredentials, AccountSuspended, Unavailable }

public sealed record LoginResult(LoginOutcome Outcome, IssuedAccessToken? Token = null,
    string? RefreshSecret = null, bool RememberMe = false)
{
    public override string ToString() => $"LoginResult {{ Outcome = {Outcome}, [credentials redacted] }}";
}

public interface ILoginStore
{
    Task<LoginResult> AuthenticateAndStartSessionAsync(ValidatedLogin login, string? previousRefreshSecret,
        CancellationToken cancellationToken);
}
