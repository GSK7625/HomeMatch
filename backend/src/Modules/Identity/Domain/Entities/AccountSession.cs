namespace HomeMatch.Modules.Identity.Domain.Entities;

/// <summary>An absolute-lived login session. Refresh credentials are stored separately.</summary>
public sealed class AccountSession
{
    private AccountSession() { }

    public AccountSession(Guid userId, long securityVersion, bool rememberMe,
        DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A session must belong to an account.", nameof(userId));
        if (securityVersion <= 0) throw new ArgumentOutOfRangeException(nameof(securityVersion));
        if (expiresAt <= createdAt) throw new ArgumentException("Session expiry must follow creation.", nameof(expiresAt));
        Id = Guid.NewGuid();
        UserId = userId;
        SecurityVersion = securityVersion;
        RememberMe = rememberMe;
        CreatedAt = createdAt.ToUniversalTime();
        ExpiresAt = expiresAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public long SecurityVersion { get; private set; }
    public bool RememberMe { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActiveAt(DateTimeOffset now) => CreatedAt <= now && now < ExpiresAt && RevokedAt is null;

    public void Revoke(DateTimeOffset now)
    {
        if (now < CreatedAt) throw new ArgumentOutOfRangeException(nameof(now));
        RevokedAt ??= now.ToUniversalTime();
    }
}
