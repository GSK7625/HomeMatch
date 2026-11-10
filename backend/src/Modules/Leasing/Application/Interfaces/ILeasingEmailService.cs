namespace HomeMatch.Modules.Leasing.Application.Interfaces;

public interface ILeasingEmailService
{
    Task SendSignatureOtpEmailAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        string otp,
        CancellationToken cancellationToken = default);

    Task SendContractSignedNotificationAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        CancellationToken cancellationToken = default);
}
