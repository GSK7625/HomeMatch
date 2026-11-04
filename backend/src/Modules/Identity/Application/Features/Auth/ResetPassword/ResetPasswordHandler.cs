using System.Net;
using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.ResetPassword;

public class ResetPasswordHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        IIdentityAccountService accountService,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<ResetPasswordHandler> logger)
    {
        _accountService = accountService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<Result<ResetPasswordResult>> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Validation cú pháp & chính sách mật khẩu (BR-06)
        var validationError = ResetPasswordValidator.Validate(command);
        if (validationError != null)
        {
            return Result.Failure<ResetPasswordResult>(validationError);
        }

        // 2. Thực hiện đặt lại mật khẩu thông qua Identity
        var rawToken = command.Token.Trim();
        if (rawToken.Contains('%'))
        {
            rawToken = WebUtility.UrlDecode(rawToken);
        }

        var result = await _accountService.ResetPasswordAsync(
            command.UserId,
            rawToken,
            command.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Đặt lại mật khẩu thất bại cho UserId {UserId}: {Errors}",
                command.UserId, string.Join(", ", result.Errors));

            return Result.Failure<ResetPasswordResult>(new Error(
                IdentityErrors.TokenInvalid,
                "Mã token đặt lại mật khẩu không hợp lệ, đã hết hạn hoặc đã được sử dụng trước đó."));
        }

        // 3. Tăng SecurityVersion và thu hồi toàn bộ phiên đăng nhập cũ (BR-15)
        await _accountService.IncrementSecurityVersionAsync(command.UserId, cancellationToken);
        await _refreshTokenRepository.RevokeAllUserTokensAsync(command.UserId, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng {UserId} đã đặt lại mật khẩu thành công. Toàn bộ phiên cũ đã bị thu hồi.", command.UserId);

        return Result.Success(new ResetPasswordResult(
            "Đặt lại mật khẩu thành công. Toàn bộ phiên làm việc cũ đã được đăng xuất an toàn. Vui lòng đăng nhập lại với mật khẩu mới."));
    }
}
