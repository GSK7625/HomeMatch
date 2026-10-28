namespace HomeMatch.Modules.Identity.Domain.Entities;

public enum EmailVerificationState { Pending = 1, Sent = 2, Failed = 3, Consumed = 4, Invalidated = 5, Locked = 6 }

public sealed class EmailVerificationChallenge
{
    public const string VerificationPurpose = "EmailVerification";
    public const int MaximumAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan PendingLease = TimeSpan.FromMinutes(2);

    private EmailVerificationChallenge() { }

    public EmailVerificationChallenge(Guid id, Guid userId, string email, string normalizedEmail,
        string codeMac, string keyId, DateTimeOffset now)
    {
        if (id == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Identifiers are required.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254
            || normalizedEmail != email.ToUpperInvariant()) throw new ArgumentException("Invalid email snapshot.");
        if (codeMac is not { Length: 64 } || !codeMac.All(char.IsAsciiHexDigit)) throw new ArgumentException("Invalid code MAC.");
        if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 32) throw new ArgumentException("Invalid key identifier.");
        if (now.Offset != TimeSpan.Zero) throw new ArgumentException("UTC is required.");
        Id = id; UserId = userId; EmailSnapshot = email; NormalizedEmailSnapshot = normalizedEmail;
        CodeMac = codeMac; KeyId = keyId; CreatedAt = now; ExpiresAt = now + Lifetime;
        PendingLeaseUntil = now + PendingLease; State = EmailVerificationState.Pending;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Purpose { get; private set; } = VerificationPurpose;
    public string EmailSnapshot { get; private set; } = string.Empty;
    public string NormalizedEmailSnapshot { get; private set; } = string.Empty;
    public string CodeMac { get; private set; } = string.Empty;
    public string KeyId { get; private set; } = string.Empty;
    public EmailVerificationState State { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset PendingLeaseUntil { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? InvalidatedAt { get; private set; }
    public int FailedAttempts { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }

    public bool IsVerifiableAt(DateTimeOffset now) => State == EmailVerificationState.Sent
        && now >= CreatedAt && now < ExpiresAt && FailedAttempts < MaximumAttempts;

    public void Submit(DateTimeOffset now)
    {
        RequireTime(now);
        if (State != EmailVerificationState.Pending || now >= ExpiresAt) throw new InvalidOperationException("Reservation is not usable.");
        State = EmailVerificationState.Sent; SubmittedAt = now;
    }

    public void EndSubmission(DateTimeOffset now, bool failed)
    {
        RequireTime(now);
        if (State != EmailVerificationState.Pending) throw new InvalidOperationException("Reservation has ended.");
        State = failed ? EmailVerificationState.Failed : EmailVerificationState.Invalidated; InvalidatedAt = now;
    }

    public void Invalidate(DateTimeOffset now)
    {
        RequireTime(now);
        if (State is not (EmailVerificationState.Pending or EmailVerificationState.Sent)) return;
        State = EmailVerificationState.Invalidated; InvalidatedAt = now;
    }

    public void RejectCode(DateTimeOffset now)
    {
        RequireTime(now);
        if (!IsVerifiableAt(now)) throw new InvalidOperationException("Challenge is not usable.");
        FailedAttempts++; LastAttemptAt = now;
        if (FailedAttempts == MaximumAttempts) { State = EmailVerificationState.Locked; InvalidatedAt = now; }
    }

    public void Consume(DateTimeOffset now)
    {
        RequireTime(now);
        if (!IsVerifiableAt(now)) throw new InvalidOperationException("Challenge is not usable.");
        State = EmailVerificationState.Consumed; ConsumedAt = now; LastAttemptAt = now;
    }

    private void RequireTime(DateTimeOffset now)
    {
        if (now.Offset != TimeSpan.Zero || now < CreatedAt) throw new ArgumentException("Invalid UTC timestamp.");
    }

    public override string ToString() => "[EmailVerificationChallenge redacted]";
}
