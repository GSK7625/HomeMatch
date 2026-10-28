namespace HomeMatch.Shared.Contracts.Notifications;

public enum EmailSubmissionOutcome { Accepted, Rejected, Unknown, Unavailable }

/// <summary>Submission only: acceptance by SMTP does not guarantee delivery to the inbox.</summary>
public interface ITransactionalEmailSender
{
    bool IsAvailable { get; }
    string SenderKey { get; }
    Task<EmailSubmissionOutcome> SendAsync(TransactionalEmail message, CancellationToken cancellationToken);
}

/// <summary>Sensitive in-memory content. Never persist or log the body.</summary>
public sealed class TransactionalEmail(string recipient, string subject, string textBody)
{
    public string Recipient { get; } = recipient;
    public string Subject { get; } = subject;
    public string TextBody { get; } = textBody;
    public override string ToString() => "[TransactionalEmail redacted]";
}
