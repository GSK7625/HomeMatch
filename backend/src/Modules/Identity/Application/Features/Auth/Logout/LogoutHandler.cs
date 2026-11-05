using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Infrastructure.Services;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.Logout;

public class LogoutHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RawRefreshToken))
        {
            return Result.Success();
        }

        var tokenHash = TokenHasher.HashToken(command.RawRefreshToken.Trim());
        var token = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (token != null && !token.IsRevoked)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Phiên đăng nhập của UserId {UserId} đã được thu hồi thành công.", token.UserId);
        }

        return Result.Success();
    }
}
