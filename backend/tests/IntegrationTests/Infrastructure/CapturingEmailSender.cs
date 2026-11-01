using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using HomeMatch.Shared.Contracts.Notifications;

namespace HomeMatch.IntegrationTests.Infrastructure;

internal sealed class CapturingEmailSender : ITransactionalEmailSender
{
    public bool IsAvailable => true;
    public string SenderKey { get; } = Guid.NewGuid().ToString("N");
    private readonly ConcurrentQueue<TransactionalEmail> _messages = new();
    public Task<EmailSubmissionOutcome> SendAsync(TransactionalEmail message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.FromResult(EmailSubmissionOutcome.Accepted);
    }
    public string LatestCode(string email) => Regex.Match(_messages.Last(x => x.Recipient.Equals(email, StringComparison.OrdinalIgnoreCase)).TextBody, "[0-9]{6}").Value;
}
