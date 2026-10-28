namespace HomeMatch.Modules.Identity.Application.Logout;

public sealed record LogoutAllSubject(Guid UserId, Guid SessionId, string Role);
public enum LogoutOutcome { Succeeded, InvalidAuthentication, Unavailable }
public sealed record LogoutResult(LogoutOutcome Outcome, bool ClearRefreshCookie = false);

public interface ILogoutSessionService
{
    Task<LogoutResult> LogoutCurrentAsync(string? rawRefreshSecret, CancellationToken cancellationToken);
    Task<LogoutResult> LogoutAllAsync(LogoutAllSubject subject, string? rawRefreshSecret, CancellationToken cancellationToken);
}
