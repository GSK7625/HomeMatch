namespace HomeMatch.Shared.Contracts.Identity;

public interface IIdentityOtpService
{
    Task<string> GenerateSignatureOtpAsync(long userId, long contractId, CancellationToken cancellationToken = default);
    Task<bool> VerifySignatureOtpAsync(long userId, long contractId, string otp, CancellationToken cancellationToken = default);
}
