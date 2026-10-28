using HomeMatch.Shared.Contracts.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HomeMatch.Modules.Notifications.Infrastructure.Email;

/// <summary>Transport seam for deterministic tests; the runtime implementation always uses mandatory STARTTLS.</summary>
public interface ISmtpSubmissionTransport
{
    Task<EmailSubmissionOutcome> SubmitAsync(GmailSmtpOptions settings, TransactionalEmail message, CancellationToken cancellationToken);
}

public sealed class MailKitSmtpTransport : ISmtpSubmissionTransport
{
    public async Task<EmailSubmissionOutcome> SubmitAsync(GmailSmtpOptions settings, TransactionalEmail message,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        using var client = new SmtpClient(); // No protocol logger, certificate bypass or singleton connection.
        try
        {
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
            mail.To.Add(new MailboxAddress(string.Empty, message.Recipient));
            mail.Subject = message.Subject;
            mail.Body = new TextPart("plain") { Text = message.TextBody };
            await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTls, deadline.Token);
            await client.AuthenticateAsync(settings.Username, settings.AppPassword, deadline.Token);
            await client.SendAsync(mail, deadline.Token);
            // Failure in QUIT must not change acceptance of DATA into a retryable result.
            try { await client.DisconnectAsync(true, deadline.Token); }
            catch (Exception exception) when (exception is IOException or MailKit.ProtocolException or OperationCanceledException or SmtpCommandException or System.Net.Sockets.SocketException or TimeoutException) { }
            return EmailSubmissionOutcome.Accepted;
        }
        catch (Exception exception) when (exception is MailKit.Security.AuthenticationException or SmtpCommandException or NotSupportedException)
        {
            return EmailSubmissionOutcome.Rejected;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return EmailSubmissionOutcome.Unknown; }
        catch (Exception exception) when (exception is IOException or MailKit.ProtocolException or TimeoutException
            or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException)
        {
            return EmailSubmissionOutcome.Unknown;
        }
    }
}
