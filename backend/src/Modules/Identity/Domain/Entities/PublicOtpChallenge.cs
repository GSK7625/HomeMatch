namespace HomeMatch.Modules.Identity.Domain.Entities;

/// <summary>Anonymous registration/recovery challenge. Secrets are represented only by MAC/hash.</summary>
public sealed class PublicOtpChallenge
{
    public Guid Id { get; set; }
    public long UserId { get; set; }
    public string Purpose { get; set; } = "";
    public string Email { get; set; } = "";
    public long SecurityVersion { get; set; }
    public string CodeMac { get; set; } = "";
    public string KeyId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Submitted { get; set; }
    public bool Closed { get; set; }
    public int FailedAttempts { get; set; }
    public string? ResetTokenHash { get; set; }
    public DateTimeOffset? ResetExpiresAt { get; set; }
    public bool ResetUsed { get; set; }
    public bool CanVerifyAt(DateTimeOffset now) => !Closed && Submitted && now >= CreatedAt
        && now < ExpiresAt && FailedAttempts < 5;

    public void RejectCode(DateTimeOffset now)
    {
        if (!CanVerifyAt(now)) throw new InvalidOperationException("Challenge is not verifiable.");
        FailedAttempts++;
        if (FailedAttempts == 5) Closed = true;
    }

    public override string ToString() => "[PublicOtpChallenge redacted]";
}
