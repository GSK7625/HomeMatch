using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Shared.Common;

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

public sealed class ResetPasswordHandler(IPublicOtpStore store, INewPasswordPolicy passwords)
{
    public Task<Result<PublicOtpResponse>> HandleAsync(string token, string password, CancellationToken cancellationToken)
    {
        if (!passwords.IsAllowed(password))
            return Task.FromResult(Result.Failure<PublicOtpResponse>(PublicOtpErrors.Validation));
        return store.ResetAsync(token, password, cancellationToken);
    }
}
