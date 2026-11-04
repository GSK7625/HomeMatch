using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.LogoutAll;

public class LogoutAllHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutAllHandler> _logger;

    public LogoutAllHandler(
        IIdentityAccountService accountService,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutAllHandler> logger)
    {
        _accountService = accountService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(LogoutAllCommand command, CancellationToken cancellationToken = default)
    {
        if (command.UserId <= 0)
        {
            return Result.Failure(new Error("Identity.ValidationFailed", "UserId không hợp lệ."));
        }

        // 1. Tăng SecurityVersion để vô hiệu hóa tất cả Access Token hiện hành
        await _accountService.IncrementSecurityVersionAsync(command.UserId, cancellationToken);

        // 2. Thu hồi toàn bộ Refresh Token trong database
        await _refreshTokenRepository.RevokeAllUserTokensAsync(command.UserId, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toàn bộ phiên đăng nhập của UserId {UserId} đã bị thu hồi.", command.UserId);
        return Result.Success();
    }
}
