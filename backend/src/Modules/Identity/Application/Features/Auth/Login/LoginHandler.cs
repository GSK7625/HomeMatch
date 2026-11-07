using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Services;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.Login;

public class LoginHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IIdentityAccountService accountService,
        IUserProfileRepository profileRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        ILogger<LoginHandler> logger)
    {
        _accountService = accountService;
        _profileRepository = profileRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<LoginResult>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Validate
        var validationError = LoginValidator.Validate(command);
        if (validationError != null)
        {
            return Result.Failure<LoginResult>(validationError);
        }

        // 2. Kiểm tra thông tin đăng nhập
        var credResult = await _accountService.VerifyCredentialsAsync(
            command.Email.Trim(),
            command.Password,
            cancellationToken);

        if (!credResult.Succeeded)
        {
            return Result.Failure<LoginResult>(new Error(
                IdentityErrors.InvalidCredentials,
                "Email hoặc mật khẩu không chính xác."));
        }

        // 3. Kiểm tra trạng thái tài khoản
        if (credResult.Status != UserStatus.Active)
        {
            return Result.Failure<LoginResult>(new Error(
                IdentityErrors.AccountSuspended,
                "Tài khoản của bạn đã bị tạm khóa. Vui lòng liên hệ quản trị viên."));
        }

        // 4. Kiểm tra xác thực email (BR-03)
        if (!credResult.EmailConfirmed)
        {
            return Result.Failure<LoginResult>(new Error(
                IdentityErrors.EmailNotVerified,
                "Địa chỉ email chưa được xác thực. Vui lòng kiểm tra hộp thư để kích hoạt tài khoản trước khi đăng nhập."));
        }

        // 5. Lấy hồ sơ người dùng để lấy họ tên
        var profile = await _profileRepository.GetByUserIdAsync(credResult.UserId, cancellationToken);
        var fullName = profile?.FullName ?? credResult.Email;

        // 6. Sinh Access Token (JWT - 15 phút)
        var accessToken = _jwtTokenService.GenerateAccessToken(
            credResult.UserId,
            credResult.Email,
            credResult.Role,
            fullName,
            credResult.SecurityVersion);

        // 7. Sinh Refresh Token (phiên 24 giờ)
        var rawRefreshToken = TokenHasher.GenerateSecureToken();
        var tokenHash = TokenHasher.HashToken(rawRefreshToken);
        var now = DateTimeOffset.UtcNow;
        var sessionExpiresAt = now.AddHours(24);

        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            UserId = credResult.UserId,
            TokenHash = tokenHash,
            TokenType = "REFRESH",
            DeviceInfo = command.DeviceInfo,
            IpAddress = command.IpAddress,
            CreatedAt = now,
            ExpiresAt = sessionExpiresAt
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng {UserId} ({Email}) đã đăng nhập thành công từ IP {IpAddress}.",
            credResult.UserId, credResult.Email, command.IpAddress ?? "unknown");

        return Result.Success(new LoginResult(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresIn: _jwtTokenService.AccessTokenExpirationSeconds,
            ExpiresAt: now.AddSeconds(_jwtTokenService.AccessTokenExpirationSeconds),
            SessionExpiresAt: sessionExpiresAt));
    }
}
