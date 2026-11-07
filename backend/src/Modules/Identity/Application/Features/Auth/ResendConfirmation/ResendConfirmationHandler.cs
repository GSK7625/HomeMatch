using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.ResendConfirmation;

public class ResendConfirmationHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ResendConfirmationHandler> _logger;

    public ResendConfirmationHandler(
        IIdentityAccountService accountService,
        IEmailSender emailSender,
        ILogger<ResendConfirmationHandler> logger)
    {
        _accountService = accountService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<ResendConfirmationResult>> HandleAsync(ResendConfirmationCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<ResendConfirmationResult>(new Error(
                "Identity.ValidationFailed",
                "Email không được để trống."));
        }

        var trimmedEmail = command.Email.Trim();

        var userId = await _accountService.GetUserIdByEmailAsync(trimmedEmail, cancellationToken);
        if (userId.HasValue)
        {
            var isConfirmed = await _accountService.IsEmailConfirmedAsync(trimmedEmail, cancellationToken);
            if (!isConfirmed)
            {
                var token = await _accountService.GenerateEmailConfirmationTokenAsync(userId.Value, cancellationToken);
                await SendConfirmationEmailAsync(trimmedEmail, userId.Value, token, cancellationToken);
                _logger.LogInformation("Đã gửi lại email xác thực cho UserId {UserId} ({Email}).", userId.Value, trimmedEmail);
            }
        }

        // Luôn trả về phản hồi chung để chống dò tìm tài khoản
        return Result.Success(new ResendConfirmationResult(
            "Nếu địa chỉ email tồn tại trên hệ thống và chưa kích hoạt, một liên kết xác thực mới đã được gửi tới hòm thư của bạn."));
    }

    private async Task SendConfirmationEmailAsync(string email, long userId, string token, CancellationToken cancellationToken)
    {
        var encodedToken = WebUtility.UrlEncode(token);
        var confirmationLink = $"http://localhost:3000/verify-email?userId={userId}&token={encodedToken}";

        var subject = "[HomeMatch] Gửi lại liên kết xác thực tài khoản";
        var body = $"""
            <h2>Chào bạn!</h2>
            <p>Bạn vừa yêu cầu gửi lại liên kết kích hoạt tài khoản HomeMatch:</p>
            <p><a href="{confirmationLink}" style="padding: 10px 15px; background-color: #2563eb; color: #fff; text-decoration: none; border-radius: 5px;">Kích hoạt tài khoản</a></p>
            <p>Hoặc truy cập link sau: <br/><code>{confirmationLink}</code></p>
            <p>Liên kết này có hiệu lực trong vòng 24 giờ.</p>
            """;

        await _emailSender.SendEmailAsync(email, subject, body, cancellationToken);
    }
}
