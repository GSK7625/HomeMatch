using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public sealed record PublicOtpRateLimitError(int RetryAfterSeconds)
    : Error("Identity.RateLimitExceeded", "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.");
