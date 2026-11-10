using System.Net;
using System.Net.Mail;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Infrastructure.Services;

public sealed class LeasingEmailService : ILeasingEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LeasingEmailService> _logger;

    public LeasingEmailService(
        IConfiguration configuration,
        ILogger<LeasingEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendSignatureOtpEmailAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[HomeMatch] Mã xác thực ký hợp đồng thuê #{contractCode}";
        var htmlBody = $"""
            <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; padding: 24px;">
                <h2 style="color: #2563eb; margin-top: 0;">HomeMatch — Xác thực ký hợp đồng</h2>
                <p>Xin chào <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
                <p>Bạn vừa yêu cầu mã xác thực để ký điện tử hợp đồng thuê phòng <strong>#{WebUtility.HtmlEncode(contractCode)}</strong>.</p>
                <div style="background-color: #f1f5f9; padding: 16px; border-radius: 6px; text-align: center; margin: 24px 0;">
                    <span style="font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #1e293b;">{otp}</span>
                </div>
                <p style="color: #475569; font-size: 14px;">Mã OTP có hiệu lực trong vòng <strong>5 phút</strong>. Tuyệt đối không chia sẻ mã này cho bất kỳ ai khác.</p>
                <p style="color: #64748b; font-size: 13px;">Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc liên hệ với bộ phận hỗ trợ HomeMatch.</p>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
                <p style="font-size: 12px; color: #94a3b8; text-align: center;">HomeMatch Platform — Giải pháp quản lý & thuê trọ thông minh</p>
            </div>
            """;

        await SendEmailInternalAsync(toEmail, subject, htmlBody, otp, cancellationToken);
    }

    public async Task SendContractSignedNotificationAsync(
        string toEmail,
        string recipientName,
        string contractCode,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[HomeMatch] Hợp đồng thuê #{contractCode} đã được hai bên ký thành công!";
        var htmlBody = $"""
            <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; padding: 24px;">
                <h2 style="color: #16a34a; margin-top: 0;">HomeMatch — Ký hợp đồng thành công</h2>
                <p>Xin chào <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
                <p>Hợp đồng thuê phòng <strong>#{WebUtility.HtmlEncode(contractCode)}</strong> đã được cả Chủ nhà và Người thuê hoàn tất ký điện tử.</p>
                <p>Hợp đồng hiện đã chuyển sang trạng thái <strong>ĐÃ KÝ (SIGNED)</strong> và sẵn sàng cho việc ghi nhận tiền cọc cũng như chuẩn bị bàn giao phòng.</p>
                <p>Bạn có thể đăng nhập vào ứng dụng HomeMatch để xem và tải về bản sao hợp đồng bất cứ lúc nào.</p>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
                <p style="font-size: 12px; color: #94a3b8; text-align: center;">HomeMatch Platform — Trân trọng thông báo</p>
            </div>
            """;

        await SendEmailInternalAsync(toEmail, subject, htmlBody, null, cancellationToken);
    }

    private async Task SendEmailInternalAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? otpForDevLog,
        CancellationToken cancellationToken)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
        var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
        var smtpPort = int.TryParse(smtpPortStr, out var port) ? port : 587;
        var senderName = _configuration["EmailSettings:SenderName"] ?? "HomeMatch Support";
        var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? string.Empty;
        var appPassword = _configuration["EmailSettings:AppPassword"] ?? string.Empty;
        var enableSsl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out var ssl) ? ssl : true;

        var isConfigured = !string.IsNullOrWhiteSpace(senderEmail) && !string.IsNullOrWhiteSpace(appPassword);

        if (!isConfigured)
        {
            if (otpForDevLog is not null)
            {
                _logger.LogWarning(
                    "📧 [DEV MOCK EMAIL] Gửi OTP tới: {ToEmail} | Subject: {Subject} | OTP Code: >>> {Otp} <<< (Chưa cấu hình Gmail AppPassword)",
                    toEmail, subject, otpForDevLog);
            }
            else
            {
                _logger.LogWarning(
                    "📧 [DEV MOCK EMAIL] Gửi thông báo tới: {ToEmail} | Subject: {Subject} (Chưa cấu hình Gmail AppPassword)",
                    toEmail, subject);
            }

            return;
        }

        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(senderEmail, appPassword.Trim()),
                EnableSsl = enableSsl
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("✅ [LeasingEmailService] Đã gửi email thành công tới {ToEmail} qua SMTP.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [LeasingEmailService] Lỗi khi gửi email tới {ToEmail}: {Message}", toEmail, ex.Message);
        }
    }
}
