using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HomeMatch.Modules.Identity.Application.Interfaces;

namespace HomeMatch.Modules.Identity.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IOptions<EmailSettings> options,
        ILogger<EmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra cấu hình Gmail SMTP
        if (!_settings.IsConfigured)
        {
            _logger.LogError("❌ EmailSettings (SenderEmail / AppPassword) chưa được cấu hình. Không thể gửi email tới {ToEmail}.", toEmail);
            return;
        }

        // 2. Gửi thật qua Gmail SMTP
        try
        {
            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.AppPassword.Trim()),
                EnableSsl = _settings.EnableSsl
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlContent,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("✅ Đã gửi email thành công tới {ToEmail} qua Gmail SMTP.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi gửi email qua Gmail SMTP tới {ToEmail}: {Message}", toEmail, ex.Message);
            // Không re-throw để tránh làm sập luồng đăng ký/quên mật khẩu nếu mạng chập chờn
        }
    }
}
