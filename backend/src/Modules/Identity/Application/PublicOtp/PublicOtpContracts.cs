using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public interface IPublicOtpStore
{
    Task<Result<PublicOtpResponse>> SendAsync(string normalizedEmail, string purpose, string? password, CancellationToken cancellationToken);
    Task<Result<PublicOtpResponse>> VerifyAsync(Guid challengeId, string code, string purpose, CancellationToken cancellationToken);
    Task<Result<PublicOtpResponse>> ResetAsync(string token, string password, CancellationToken cancellationToken);
}

public sealed record PublicOtpResponse(string Status, Guid? ChallengeId = null, DateTimeOffset? ExpiresAt = null,
    string? ResetToken = null, int RetryAfterSeconds = 0)
{
    public override string ToString() => $"PublicOtpResponse {Status} [redacted]";
}

public sealed record PublicOtpRateLimitError(int RetryAfterSeconds)
    : Error("Identity.RateLimitExceeded", "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.");

public static class PublicOtpErrors
{
    public static readonly Error Invalid = new("Identity.OtpInvalid", "Mã xác thực hoặc quyền đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
    public static readonly Error Validation = new("Identity.ValidationFailed", "Dữ liệu yêu cầu không hợp lệ.");
    public static readonly Error Unavailable = new("Server.DependencyUnavailable", "Dịch vụ tạm thời chưa sẵn sàng.");
}
