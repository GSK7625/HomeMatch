namespace HomeMatch.Modules.Identity.Domain.Entities;

/// <summary>Server-side credential metadata. Raw refresh secrets are never persisted.</summary>
public sealed class RefreshCredential
{
    private RefreshCredential() { }

    public RefreshCredential(Guid sessionId, string tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("A refresh credential needs a session.", nameof(sessionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        if (tokenHash.Length != 64 || tokenHash.Any(character => character is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("Expected a lowercase SHA-256 hash.", nameof(tokenHash));
        if (expiresAt <= createdAt) throw new ArgumentException("Credential expiry must follow creation.", nameof(expiresAt));
        Id = Guid.NewGuid();
        SessionId = sessionId;
        TokenHash = tokenHash;
        CreatedAt = createdAt.ToUniversalTime();
        ExpiresAt = expiresAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByCredentialId { get; private set; }

    public bool IsUsableAt(DateTimeOffset now)
        => CreatedAt <= now && now < ExpiresAt && UsedAt is null && RevokedAt is null;

    public void Consume(DateTimeOffset now)
    {
        if (!IsUsableAt(now)) throw new InvalidOperationException("Refresh credential is not usable.");
        UsedAt = now.ToUniversalTime();
    }

    public void LinkReplacement(RefreshCredential replacement)
    {
        if (UsedAt is null || ReplacedByCredentialId is not null || replacement.Id == Id
            || replacement.SessionId != SessionId || replacement.CreatedAt < UsedAt.Value
            || replacement.ExpiresAt != ExpiresAt || replacement.UsedAt is not null || replacement.RevokedAt is not null)
            throw new InvalidOperationException("Invalid refresh credential replacement.");
        ReplacedByCredentialId = replacement.Id;
    }
}
