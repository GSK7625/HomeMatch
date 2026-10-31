using HomeMatch.Shared.Common;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Application.Registration;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public sealed class VerifyPublicOtpHandler(IPublicOtpStore store)
{
    public Task<Result<PublicOtpResponse>> HandleAsync(Guid challengeId, string code, string purpose, CancellationToken cancellationToken)
    {
        if (challengeId == Guid.Empty || code is not { Length: 6 } || !code.All(char.IsAsciiDigit)
            || purpose is not ("Registration" or "PasswordReset"))
            return Task.FromResult(Result.Failure<PublicOtpResponse>(PublicOtpErrors.Validation));
        return store.VerifyAsync(challengeId, code, purpose, cancellationToken);
    }
}
