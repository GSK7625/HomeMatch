using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Services;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityAccountService _accountService;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityAccountService accountService,
        IUserProfileRepository profileRepository,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _accountService = accountService;
        _profileRepository = profileRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResult>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RawRefreshToken))
        {
            return Result.Failure<RefreshTokenResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Refresh token không được để trống."));
        }

        var incomingTokenHash = TokenHasher.HashToken(command.RawRefreshToken.Trim());
        var tokenEntity = await _refreshTokenRepository.GetByHashAsync(incomingTokenHash, cancellationToken);

        if (tokenEntity == null)
        {
            return Result.Failure<RefreshTokenResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phiên đăng nhập không hợp lệ hoặc không tồn tại."));
        }

        // Phát hiện Replay Token (Token Reuse Detection)
        if (tokenEntity.IsRevoked)
        {
            _logger.LogWarning("Cảnh báo an ninh: Phát hiện tái sử dụng Refresh Token đã bị thu hồi của UserId {UserId}!",
                tokenEntity.UserId);

            // Vô hiệu hóa toàn bộ phiên của người dùng để bảo vệ tài khoản
            await _refreshTokenRepository.RevokeAllUserTokensAsync(tokenEntity.UserId, cancellationToken);
            await _accountService.IncrementSecurityVersionAsync(tokenEntity.UserId, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            return Result.Failure<RefreshTokenResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phát hiện dấu hiệu truy cập bất thường. Toàn bộ phiên đăng nhập đã bị vô hiệu hóa vì lý do an toàn."));
        }

        // Kiểm tra hết hạn
        if (tokenEntity.IsExpired)
        {
            return Result.Failure<RefreshTokenResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."));
        }

        // Kiểm tra thông tin người dùng
        var user = await _accountService.GetUserDetailsByIdAsync(tokenEntity.UserId, cancellationToken);
        if (user == null || user.Status != UserStatus.Active)
        {
            return Result.Failure<RefreshTokenResult>(new Error(
                IdentityErrors.AccountSuspended,
                "Tài khoản người dùng không hoạt động."));
        }

        // Xoay vòng Token (Token Rotation)
        var newRawRefreshToken = TokenHasher.GenerateSecureToken();
        var newTokenHash = TokenHasher.HashToken(newRawRefreshToken);
        var now = DateTimeOffset.UtcNow;

        tokenEntity.RevokedAt = now;
        tokenEntity.ReplacedByTokenHash = newTokenHash;

        var newTokenEntity = new Domain.Entities.RefreshToken
        {
            UserId = user.UserId,
            TokenHash = newTokenHash,
            TokenType = "REFRESH",
            DeviceInfo = command.DeviceInfo ?? tokenEntity.DeviceInfo,
            IpAddress = command.IpAddress ?? tokenEntity.IpAddress,
            CreatedAt = now,
            ExpiresAt = tokenEntity.ExpiresAt // Giữ hạn tuyệt đối của phiên
        };

        await _refreshTokenRepository.AddAsync(newTokenEntity, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // Lấy họ tên người dùng cho claim
        var profile = await _profileRepository.GetByUserIdAsync(user.UserId, cancellationToken);
        var fullName = profile?.FullName ?? user.Email;

        // Cấp JWT Access Token mới (15 phút)
        var newAccessToken = _jwtTokenService.GenerateAccessToken(
            user.UserId,
            user.Email,
            user.Role,
            fullName,
            user.SecurityVersion);

        _logger.LogInformation("Đã làm mới token thành công cho UserId {UserId}.", user.UserId);

        return Result.Success(new RefreshTokenResult(
            AccessToken: newAccessToken,
            RefreshToken: newRawRefreshToken,
            ExpiresIn: _jwtTokenService.AccessTokenExpirationSeconds,
            ExpiresAt: now.AddSeconds(_jwtTokenService.AccessTokenExpirationSeconds),
            SessionExpiresAt: tokenEntity.ExpiresAt));
    }
}
