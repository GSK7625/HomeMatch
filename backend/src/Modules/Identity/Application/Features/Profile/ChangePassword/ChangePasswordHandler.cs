using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Profile.ChangePassword;

public class ChangePasswordHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IIdentityAccountService accountService,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<ChangePasswordHandler> logger)
    {
        _accountService = accountService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<Result<ChangePasswordResult>> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ChangePasswordValidator.Validate(command);
        if (validationError is not null)
        {
            return Result.Failure<ChangePasswordResult>(validationError);
        }

        var user = await _accountService.GetUserDetailsByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<ChangePasswordResult>(new Error(
                IdentityErrors.AuthenticationRequired,
                "Tài khoản không tồn tại."));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<ChangePasswordResult>(new Error(
                IdentityErrors.AccountSuspended,
                "Tài khoản đã bị tạm khóa."));
        }

        if (command.TokenSecurityVersion.HasValue && command.TokenSecurityVersion.Value < user.SecurityVersion)
        {
            return Result.Failure<ChangePasswordResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phiên đăng nhập đã bị vô hiệu hóa. Vui lòng đăng nhập lại."));
        }

        var changeResult = await _accountService.ChangePasswordAsync(
            user.UserId,
            command.CurrentPassword,
            command.NewPassword,
            cancellationToken);

        if (!changeResult.Succeeded)
        {
            var isWrongCurrentPassword = changeResult.Errors.Any(e =>
                string.Equals(e, "PasswordMismatch", StringComparison.OrdinalIgnoreCase));

            if (isWrongCurrentPassword)
            {
                return Result.Failure<ChangePasswordResult>(new Error(
                    IdentityErrors.InvalidCredentials,
                    "Mật khẩu hiện tại không chính xác."));
            }

            _logger.LogWarning("Đổi mật khẩu thất bại cho UserId {UserId}: {Errors}",
                user.UserId, string.Join(", ", changeResult.Errors));

            return Result.Failure<ChangePasswordResult>(new Error(
                IdentityErrors.ValidationFailed,
                "Không thể đổi mật khẩu. Vui lòng kiểm tra lại thông tin mật khẩu mới."));
        }

        // Tăng SecurityVersion và thu hồi toàn bộ refresh token cũ (BR-15)
        await _accountService.IncrementSecurityVersionAsync(user.UserId, cancellationToken);
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.UserId, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng {UserId} đã đổi mật khẩu thành công. Toàn bộ phiên cũ đã bị thu hồi.", user.UserId);

        return Result.Success(new ChangePasswordResult(
            "Đổi mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới."));
    }
}
