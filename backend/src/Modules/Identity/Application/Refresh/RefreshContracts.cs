using HomeMatch.Modules.Identity.Application.Authentication;

namespace HomeMatch.Modules.Identity.Application.Refresh;

public enum RefreshOutcome { Succeeded, InvalidSession, RateLimited, Unavailable }

public sealed record RefreshResult(RefreshOutcome Outcome, IssuedAccessToken? Token = null,
    string? RefreshSecret = null, bool RememberMe = false, TimeSpan? RetryAfter = null, DateTime? RefreshExpiresAt = null)
{
    public override string ToString() => $"RefreshResult {{ Outcome = {Outcome}, [credentials redacted] }}";
}

/// <summary>Atomic refresh use case; HTTP details and cookie access stay in Presentation.</summary>
public interface IRefreshSessionService
{
    Task<RefreshResult> RefreshAsync(string? rawSecret, string clientAddress, CancellationToken cancellationToken);
}

public interface IRefreshRequestQuota
{
    TimeSpan? TryAcquire(string clientAddress, Guid? sessionId);
}
