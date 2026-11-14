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

    Task SendContractExpiringReminderEmailAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        DateOnly endDate,
        int daysRemaining,
        CancellationToken cancellationToken = default);

    Task SendSignatureExpiredEmailAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        CancellationToken cancellationToken = default);

    Task SendContractExpiredPendingHandoverEmailAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        DateTimeOffset? handoverDueAt,
        CancellationToken cancellationToken = default);

    Task SendHandoverOverdueEmailAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        CancellationToken cancellationToken = default);
}
