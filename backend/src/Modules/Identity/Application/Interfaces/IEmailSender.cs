namespace HomeMatch.Modules.Identity.Application.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default);
}
