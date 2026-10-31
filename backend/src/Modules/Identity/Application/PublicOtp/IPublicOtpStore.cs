using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public interface IPublicOtpStore
{
    Task<Result<PublicOtpResponse>> SendAsync(string normalizedEmail, string purpose, string? password, CancellationToken cancellationToken);
    Task<Result<PublicOtpResponse>> VerifyAsync(Guid challengeId, string code, string purpose, CancellationToken cancellationToken);
    Task<Result<PublicOtpResponse>> ResetAsync(string token, string password, CancellationToken cancellationToken);
}
