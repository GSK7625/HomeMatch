using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        IIdentityAccountService accountService,
        IEmailSender emailSender,
        ILogger<ForgotPasswordHandler> logger)
    {
        _accountService = accountService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<ForgotPasswordResult>> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<ForgotPasswordResult>(new Error(
                IdentityErrors.ValidationFailed,
                "Email không được để trống."));
        }

        var trimmedEmail = command.Email.Trim();

        try
        {
            var addr = new MailAddress(trimmedEmail);
            if (addr.Address != trimmedEmail)
            {
                return Result.Failure<ForgotPasswordResult>(new Error(
                    IdentityErrors.ValidationFailed,
                    "Định dạng email không hợp lệ."));
            }
        }
        catch
        {
            return Result.Failure<ForgotPasswordResult>(new Error(
                IdentityErrors.ValidationFailed,
                "Định dạng email không hợp lệ."));
        }

        var userId = await _accountService.GetUserIdByEmailAsync(trimmedEmail, cancellationToken);
        if (userId.HasValue)
        {
            var user = await _accountService.GetUserDetailsByIdAsync(userId.Value, cancellationToken);
            // Chỉ gửi link cho tài khoản Active và đã xác thực email (BR-13)
            if (user != null && user.Status == UserStatus.Active && user.EmailConfirmed)
            {
                var token = await _accountService.GeneratePasswordResetTokenAsync(user.UserId, cancellationToken);
                await SendResetPasswordEmailAsync(user.Email, user.UserId, token, cancellationToken);
                _logger.LogInformation("Đã gửi email khôi phục mật khẩu cho UserId {UserId} ({Email}).", user.UserId, user.Email);
            }
            else
            {
                _logger.LogWarning("Yêu cầu khôi phục mật khẩu bị bỏ qua: Tài khoản UserId {UserId} chưa kích hoạt email hoặc đang bị khóa.", userId.Value);
            }
        }

        // Luôn trả phản hồi chung để chống dò email
        return Result.Success(new ForgotPasswordResult(
            "Nếu địa chỉ email tồn tại trên hệ thống và đã được kích hoạt, hướng dẫn đặt lại mật khẩu đã được gửi đến hòm thư của bạn."));
    }

    private async Task SendResetPasswordEmailAsync(string email, long userId, string token, CancellationToken cancellationToken)
    {
        var encodedToken = WebUtility.UrlEncode(token);
        var resetLink = $"http://localhost:3000/reset-password?userId={userId}&token={encodedToken}";

        var subject = "[HomeMatch] Hướng dẫn đặt lại mật khẩu";
        var body = $"""
            <h2>Yêu cầu đặt lại mật khẩu</h2>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản HomeMatch của bạn.</p>
            <p>Vui lòng nhấn vào liên kết bên dưới để tạo mật khẩu mới:</p>
            <p><a href="{resetLink}" style="padding: 10px 15px; background-color: #dc2626; color: #fff; text-decoration: none; border-radius: 5px;">Đặt lại mật khẩu</a></p>
            <p>Hoặc truy cập trực tiếp qua đường dẫn: <br/><code>{resetLink}</code></p>
            <p>Liên kết này có hiệu lực trong vòng 15 phút. Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email.</p>
            """;

        await _emailSender.SendEmailAsync(email, subject, body, cancellationToken);
    }
}
