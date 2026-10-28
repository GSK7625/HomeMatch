using System.Security.Cryptography;
using System.Text;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Notifications.Infrastructure.Email;

public sealed class GmailSmtpEmailSender(IOptions<GmailSmtpOptions> configured, ISmtpSubmissionTransport transport,
    ILogger<GmailSmtpEmailSender> logger) : ITransactionalEmailSender
{
    public bool IsAvailable => configured.Value.Enabled;
    public string SenderKey => "gmail:" + Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(configured.Value.Username.Trim().ToUpperInvariant())));

    public async Task<EmailSubmissionOutcome> SendAsync(TransactionalEmail message, CancellationToken cancellationToken)
    {
        if (!IsAvailable) return EmailSubmissionOutcome.Unavailable;
        if (!GmailSmtpOptionsValidator.IsSingleAddress(message.Recipient)
            || string.IsNullOrWhiteSpace(message.Subject) || message.Subject.Length > 200 || message.Subject.Any(char.IsControl)
            || string.IsNullOrWhiteSpace(message.TextBody) || message.TextBody.Length > 8192)
            return EmailSubmissionOutcome.Rejected;
        var outcome = await transport.SubmitAsync(configured.Value, message, cancellationToken);
        if (outcome != EmailSubmissionOutcome.Accepted)
            logger.LogWarning("Transactional email submission was not confirmed. Outcome: {Outcome}", outcome);
        return outcome;
    }
}
