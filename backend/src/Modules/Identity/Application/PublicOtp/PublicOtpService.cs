using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public sealed class PublicOtpService(SendPublicOtpHandler send, VerifyPublicOtpHandler verify, ResetPasswordHandler reset) : IPublicOtpService
{
    public Task<Result<PublicOtpResponse>> SendAsync(string email, string purpose, string? password, CancellationToken cancellationToken)
        => send.HandleAsync(email, purpose, password, cancellationToken);
    public Task<Result<PublicOtpResponse>> VerifyAsync(Guid challengeId, string code, string purpose, CancellationToken cancellationToken)
        => verify.HandleAsync(challengeId, code, purpose, cancellationToken);
    public Task<Result<PublicOtpResponse>> ResetAsync(string token, string password, CancellationToken cancellationToken)
        => reset.HandleAsync(token, password, cancellationToken);
}
