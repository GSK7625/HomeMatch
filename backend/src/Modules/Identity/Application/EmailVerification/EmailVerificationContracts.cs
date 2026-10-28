namespace HomeMatch.Modules.Identity.Application.EmailVerification;

public sealed record VerificationSubject(Guid UserId, Guid SessionId, string Role);
public enum VerificationOutcome { Reserved, Submitted, AlreadyVerified, Verified, InvalidCode, InvalidAuthentication, RateLimited, Unavailable }
public enum VerificationDeliveryOutcome { Accepted, Rejected, Unknown, Unavailable }

public sealed record VerificationResult(VerificationOutcome Outcome, Guid? ChallengeId = null,
    DateTimeOffset? ExpiresAt = null, TimeSpan? RetryAfter = null, PendingVerification? Reservation = null);

/// <summary>Only for the request use case. Do not serialize this sensitive object into an HTTP response.</summary>
public sealed class PendingVerification(Guid id, string email, string code)
{
    public Guid Id { get; } = id;
    public string Email { get; } = email;
    public string Code { get; } = code;
    public override string ToString() => "[PendingVerification redacted]";
}

public interface IEmailVerificationStore
{
    Task<VerificationResult> ReserveAsync(VerificationSubject subject, bool deliveryAvailable, string senderKey, CancellationToken cancellationToken);
    Task<VerificationResult> CompleteAsync(VerificationSubject subject, Guid challengeId, VerificationDeliveryOutcome outcome, CancellationToken cancellationToken);
    Task<VerificationResult> VerifyAsync(VerificationSubject subject, Guid challengeId, string code, CancellationToken cancellationToken);
}

public interface IEmailVerificationDelivery
{
    bool IsAvailable { get; }
    string SenderKey { get; }
    Task<VerificationDeliveryOutcome> SendAsync(PendingVerification reservation, CancellationToken cancellationToken);
}
