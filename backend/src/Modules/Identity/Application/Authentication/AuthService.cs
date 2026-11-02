using HomeMatch.Modules.Identity.Application.DTOs;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Application.Logout;
using HomeMatch.Modules.Identity.Application.Refresh;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Application.Authentication;

public sealed class AuthService(ILoginStore loginStore, IRegistrationStore registrationStore,
    INewPasswordPolicy passwords, IRefreshSessionStore refreshSessions, ILogoutSessionStore logoutSessions)
{
    public Task<LoginResult> LoginAsync(LoginRequestDto request, string? previousRefreshSecret,
        CancellationToken cancellationToken)
    {
        // Verify existing credentials without applying today's password creation rules.
        if (!AccountInput.TryNormalizeEmail(request.Email, out _, out var normalized)
            || request.Password is null || request.Password.Length > 256
            || !AccountInput.TryCountScalars(request.Password, out var length) || length is < 1 or > 128)
            return Task.FromResult(new LoginResult(LoginOutcome.InvalidInput));

        return loginStore.AuthenticateAndStartSessionAsync(new ValidatedLogin(normalized, request.Password),
            previousRefreshSecret, cancellationToken);
    }

    public Task<RegistrationOutcome> RegisterAsync(RegistrationRequestDto request, CancellationToken cancellationToken)
    {
        if (!AccountInput.TryNormalizeEmail(request.Email, out var email, out var normalized)
            || !AccountInput.TryNormalizeName(request.FullName, out var name)
            || !AccountRoles.CanSelfRegister(request.Role) || !passwords.IsAllowed(request.Password))
            return Task.FromResult(RegistrationOutcome.Invalid);

        return registrationStore.CreateIfAbsentAsync(new ValidatedRegistration(email, normalized,
            request.Password!, name, request.Role!), cancellationToken);
    }

    public Task<RefreshResult> RefreshAsync(string? rawSecret, string clientAddress, CancellationToken cancellationToken)
        => refreshSessions.RefreshAsync(rawSecret, clientAddress, cancellationToken);

    public Task<LogoutResult> LogoutCurrentAsync(string? rawRefreshSecret, CancellationToken cancellationToken)
        => logoutSessions.LogoutCurrentAsync(rawRefreshSecret, cancellationToken);

    public Task<LogoutResult> LogoutAllAsync(LogoutAllSubject subject, string? rawRefreshSecret,
        CancellationToken cancellationToken)
        => logoutSessions.LogoutAllAsync(subject, rawRefreshSecret, cancellationToken);
}
