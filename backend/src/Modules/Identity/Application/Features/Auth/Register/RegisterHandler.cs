using System.Net;
using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.Register;

public class RegisterHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        IIdentityAccountService accountService,
        IUserProfileRepository profileRepository,
        IEmailSender emailSender,
        ILogger<RegisterHandler> logger)
    {
        _accountService = accountService;
        _profileRepository = profileRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<RegisterResult>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Validation (BR-04, BR-05, BR-06, BR-07)
        var validationError = RegisterValidator.Validate(command);
        if (validationError != null)
        {
            return Result.Failure<RegisterResult>(validationError);
        }

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var rawEmail = command.Email.Trim();

        // 2. Kiểm tra tài khoản đã tồn tại (BR-05 & bảo vệ User Enumeration)
        var emailExists = await _accountService.CheckEmailExistsAsync(rawEmail, cancellationToken);
        if (emailExists)
        {
            var isConfirmed = await _accountService.IsEmailConfirmedAsync(rawEmail, cancellationToken);
            if (!isConfirmed)
            {
                // Tái sinh token và gửi lại email kích hoạt
                var existingUserId = await _accountService.GetUserIdByEmailAsync(rawEmail, cancellationToken);
                if (existingUserId.HasValue)
                {
                    var resendToken = await _accountService.GenerateEmailConfirmationTokenAsync(existingUserId.Value, cancellationToken);
                    await SendConfirmationEmailAsync(rawEmail, existingUserId.Value, resendToken, cancellationToken);
                }
            }

            // Trả về phản hồi chung để chống dò email
            return Result.Success(new RegisterResult("Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản."));
        }

        // 3. Tạo tài khoản trong Identity
        var createResult = await _accountService.CreateUserAsync(
            rawEmail,
            command.Password,
            command.Role.Trim(),
            cancellationToken);

        if (!createResult.Succeeded)
        {
            _logger.LogWarning("Tạo tài khoản thất bại cho email {Email}: {Errors}",
                rawEmail, string.Join(", ", createResult.Errors));

            var errorDetails = createResult.Errors
                .Select(err => new ErrorDetail(nameof(command.Password), err))
                .ToList();

            return Result.Failure<RegisterResult>(Error.Validation(
                code: "Identity.ValidationFailed",
                message: "Không thể tạo tài khoản người dùng.",
                details: errorDetails));
        }

        // 4. Tạo hồ sơ người dùng (UserProfile)
        var userProfile = new UserProfile
        {
            UserId = createResult.UserId,
            FullName = command.FullName.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _profileRepository.AddAsync(userProfile, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        // 5. Sinh token xác thực email và gửi email
        var token = await _accountService.GenerateEmailConfirmationTokenAsync(createResult.UserId, cancellationToken);
        await SendConfirmationEmailAsync(rawEmail, createResult.UserId, token, cancellationToken);

        _logger.LogInformation("Người dùng {UserId} ({Email}) đã đăng ký thành công với vai trò {Role}.",
            createResult.UserId, rawEmail, command.Role);

        return Result.Success(new RegisterResult("Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản."));
    }

    private async Task SendConfirmationEmailAsync(string email, long userId, string token, CancellationToken cancellationToken)
    {
        var encodedToken = WebUtility.UrlEncode(token);
        var confirmationLink = $"http://localhost:3000/verify-email?userId={userId}&token={encodedToken}";

        var subject = "[HomeMatch] Xác thực tài khoản của bạn";
        var body = $"""
            <h2>Chào mừng bạn đến với HomeMatch!</h2>
            <p>Vui lòng nhấn vào liên kết bên dưới để xác thực địa chỉ email và kích hoạt tài khoản của bạn:</p>
            <p><a href="{confirmationLink}" style="padding: 10px 15px; background-color: #2563eb; color: #fff; text-decoration: none; border-radius: 5px;">Kích hoạt tài khoản</a></p>
            <p>Hoặc truy cập trực tiếp qua đường dẫn: <br/><code>{confirmationLink}</code></p>
            <p>Liên kết này có hiệu lực trong vòng 24 giờ.</p>
            """;

        await _emailSender.SendEmailAsync(email, subject, body, cancellationToken);
    }
}
