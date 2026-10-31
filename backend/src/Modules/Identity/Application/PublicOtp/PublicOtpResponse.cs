using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public sealed record PublicOtpResponse(string Status, Guid? ChallengeId = null, DateTimeOffset? ExpiresAt = null,
    string? ResetToken = null, int RetryAfterSeconds = 0)
{
    public override string ToString() => $"PublicOtpResponse {Status} [redacted]";
}
