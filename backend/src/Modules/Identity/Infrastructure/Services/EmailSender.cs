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
        // 1. Nếu chưa điền cấu hình Google App Password: tự động fallback in ra Console/Log để dev không bị crash
        if (!_settings.IsConfigured)
        {
            _logger.LogWarning("⚠️ EmailSettings (SenderEmail / AppPassword) chưa được cấu hình trong appsettings. Tự động ghi nội dung email vào log:");
            _logger.LogInformation("================== EMAIL MOCK (CHẾ ĐỘ DEV) ==================");
            _logger.LogInformation("Gửi tới: {ToEmail}", toEmail);
            _logger.LogInformation("Tiêu đề: {Subject}", subject);
            _logger.LogInformation("Nội dung: {Content}", htmlContent);
            _logger.LogInformation("=============================================================");
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
