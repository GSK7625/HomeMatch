using HomeMatch.Shared.Common;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Application.Registration;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public sealed class SendPublicOtpHandler(IPublicOtpStore store)
{
    public Task<Result<PublicOtpResponse>> HandleAsync(string email, string purpose, string? password, CancellationToken cancellationToken)
    {
        if (!AccountInput.TryNormalizeEmail(email, out _, out var normalizedEmail)
            || purpose is not ("Registration" or "PasswordReset")
            || (purpose == "Registration" && (string.IsNullOrEmpty(password) || password.Length > 256)))
            return Task.FromResult(Result.Failure<PublicOtpResponse>(PublicOtpErrors.Validation));
        return store.SendAsync(normalizedEmail, purpose, password, cancellationToken);
    }
}
