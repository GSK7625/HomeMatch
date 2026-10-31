using HomeMatch.Shared.Common;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Application.Registration;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public sealed class ResetPasswordHandler(IPublicOtpStore store, INewPasswordPolicy passwords)
{
    public Task<Result<PublicOtpResponse>> HandleAsync(string token, string password, CancellationToken cancellationToken)
    {
        if (!passwords.IsAllowed(password))
            return Task.FromResult(Result.Failure<PublicOtpResponse>(PublicOtpErrors.Validation));
        return store.ResetAsync(token, password, cancellationToken);
    }
}
