using System.Net;
using System.Net.Sockets;
using System.Text;
using HomeMatch.Modules.Notifications.Infrastructure.Email;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.IntegrationTests;

public sealed class GmailSmtpTests
{
    private static GmailSmtpOptions Settings() => new()
    {
        Enabled = true, Username = "sender@gmail.com", FromAddress = "sender@gmail.com", AppPassword = "abcdefghijklmnop"
    };

    [Theory]
    [InlineData("host")]
    [InlineData("port")]
    [InlineData("tls")]
    [InlineData("password")]
    [InlineData("from")]
    [InlineData("header")]
    [InlineData("timeout")]
    public void EnabledGmailRejectsUnsafeConfiguration(string kind)
    {
        var settings = Settings();
        switch (kind)
        {
            case "host": settings.Host = "other.example.test"; break;
            case "port": settings.Port = 25; break;
            case "tls": settings.Security = "StartTlsWhenAvailable"; break;
            case "password": settings.AppPassword = "abcd efgh ijkl mnop"; break;
            case "from": settings.FromAddress = "other@gmail.com"; break;
            case "header": settings.FromName = "HomeMatch\r\nBcc: someone"; break;
            case "timeout": settings.TimeoutSeconds = 60; break;
        }
        var result = new GmailSmtpOptionsValidator().Validate(null, settings);
        Assert.True(result.Failed);
        Assert.DoesNotContain(settings.AppPassword, result.FailureMessage!);
    }

    [Fact]
    public async Task DisabledSenderDoesNotTouchTransportAndUsesNoFakeSuccess()
    {
        var transport = new Transport();
        var sender = new GmailSmtpEmailSender(Options.Create(new GmailSmtpOptions()), transport, new CaptureLogger());
        Assert.False(sender.IsAvailable);
        Assert.Equal(EmailSubmissionOutcome.Unavailable, await sender.SendAsync(new("recipient@example.test", "Verify", "secret"), default));
        Assert.Equal(0, transport.Calls);
        Assert.True(new GmailSmtpOptionsValidator().Validate(null, new()).Succeeded);
    }

    [Theory]
    [InlineData(EmailSubmissionOutcome.Accepted)]
    [InlineData(EmailSubmissionOutcome.Rejected)]
    [InlineData(EmailSubmissionOutcome.Unknown)]
    public async Task SenderPreservesSubmissionOutcomeAndNeverLogsContent(EmailSubmissionOutcome outcome)
    {
        var transport = new Transport { Outcome = outcome };
        var log = new CaptureLogger();
        var sender = new GmailSmtpEmailSender(Options.Create(Settings()), transport, log);
        var message = new TransactionalEmail("recipient@example.test", "Verify email", "OTP 038251");
        Assert.Equal(outcome, await sender.SendAsync(message, default));
        Assert.Equal(1, transport.Calls);
        var rendered = string.Join("\n", log.Messages);
        Assert.DoesNotContain("038251", rendered); Assert.DoesNotContain(message.Recipient, rendered);
        Assert.DoesNotContain(Settings().AppPassword, rendered);
        Assert.DoesNotContain("038251", message.ToString());
        Assert.DoesNotContain(Settings().AppPassword, Settings().ToString());
    }

    [Theory]
    [InlineData("recipient@example.test\r\nRCPT TO: injected", "Verify")]
    [InlineData("recipient@example.test", "Verify\r\nBcc: injected")]
    public async Task HeaderInjectionNeverReachesTransport(string recipient, string subject)
    {
        var transport = new Transport();
        var sender = new GmailSmtpEmailSender(Options.Create(Settings()), transport, new CaptureLogger());
        Assert.Equal(EmailSubmissionOutcome.Rejected, await sender.SendAsync(new(recipient, subject, "OTP"), default));
        Assert.Equal(0, transport.Calls);
    }

    [Fact]
    public async Task RealMailKitTransportNeverAuthenticatesWhenServerCannotOfferStartTls()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var received = new List<string>();
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync(deadline.Token);
            await using var stream = connection.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
            await writer.WriteLineAsync("220 localhost test SMTP");
            var ehlo = await reader.ReadLineAsync(deadline.Token);
            received.Add(ehlo!);
            await writer.WriteLineAsync("250-localhost");
            await writer.WriteLineAsync("250 AUTH PLAIN LOGIN");
            try
            {
                while (await reader.ReadLineAsync(deadline.Token) is { } command) received.Add(command);
            }
            catch (IOException) { }
        });
        var settings = Settings(); settings.Host = "127.0.0.1"; settings.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var outcome = await new MailKitSmtpTransport().SubmitAsync(settings, new("recipient@example.test", "Verify", "OTP"), deadline.Token);
        await server;
        Assert.Equal(EmailSubmissionOutcome.Rejected, outcome);
        Assert.NotEmpty(received);
        Assert.DoesNotContain(received, value => value.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Transport : ISmtpSubmissionTransport
    {
        public int Calls;
        public EmailSubmissionOutcome Outcome = EmailSubmissionOutcome.Accepted;
        public Task<EmailSubmissionOutcome> SubmitAsync(GmailSmtpOptions settings, TransactionalEmail message, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls); return Task.FromResult(Outcome);
        }
    }
    private sealed class CaptureLogger : ILogger<GmailSmtpEmailSender>
    {
        public readonly List<string> Messages = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
