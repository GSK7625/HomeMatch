using Microsoft.Extensions.Options;
using MimeKit;

namespace HomeMatch.Modules.Notifications.Infrastructure.Email;

public sealed class GmailSmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Security { get; set; } = "StartTls";
    public string Username { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "HomeMatch";
    public int TimeoutSeconds { get; set; } = 15;
    public override string ToString() => "[GmailSmtpOptions redacted]";
}

public sealed class GmailSmtpOptionsValidator : IValidateOptions<GmailSmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, GmailSmtpOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;
        if (options.Host != "smtp.gmail.com" || options.Port != 587 || options.Security != "StartTls"
            || options.TimeoutSeconds != 15)
            return ValidateOptionsResult.Fail("Gmail SMTP requires smtp.gmail.com:587, mandatory STARTTLS and a 15 second deadline.");
        if (!IsSingleAddress(options.Username) || !IsSingleAddress(options.FromAddress)
            || !options.Username.Equals(options.FromAddress, StringComparison.OrdinalIgnoreCase)
            || options.AppPassword.Length != 16 || !options.AppPassword.All(char.IsAsciiLetterOrDigit)
            || string.IsNullOrWhiteSpace(options.FromName) || options.FromName.Length > 100 || options.FromName.Any(char.IsControl))
            return ValidateOptionsResult.Fail("Gmail sender credentials are missing or invalid; configure an App Password without display spaces.");
        return ValidateOptionsResult.Success;
    }

    public static bool IsSingleAddress(string value) => value.Length is > 0 and <= 254 && !value.Any(char.IsControl)
        && MailboxAddress.TryParse(value, out var parsed) && parsed.Address == value && string.IsNullOrEmpty(parsed.Name);
}
