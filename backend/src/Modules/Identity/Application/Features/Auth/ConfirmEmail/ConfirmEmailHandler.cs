using System.Net;
using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.ConfirmEmail;

public class ConfirmEmailHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly ILogger<ConfirmEmailHandler> _logger;

    public ConfirmEmailHandler(
        IIdentityAccountService accountService,
        ILogger<ConfirmEmailHandler> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<Result<ConfirmEmailResult>> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken = default)
    {
        if (command.UserId <= 0)
        {
            return Result.Failure<ConfirmEmailResult>(new Error(
                "Identity.ValidationFailed",
                "Định danh người dùng không hợp lệ."));
        }

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Failure<ConfirmEmailResult>(new Error(
                "Identity.ValidationFailed",
                "Mã token xác thực không được để trống."));
        }

        var rawToken = command.Token.Trim();
        if (rawToken.Contains('%'))
        {
            rawToken = WebUtility.UrlDecode(rawToken);
        }

        var result = await _accountService.ConfirmEmailAsync(command.UserId, rawToken, cancellationToken);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Xác thực email thất bại cho UserId {UserId}: {Errors}",
                command.UserId, string.Join(", ", result.Errors));

            return Result.Failure<ConfirmEmailResult>(new Error(
                "Identity.TokenInvalid",
                "Mã xác thực không hợp lệ, đã hết hạn hoặc đã được sử dụng trước đó."));
        }

        _logger.LogInformation("Xác thực email thành công cho UserId {UserId}.", command.UserId);
        return Result.Success(new ConfirmEmailResult("Xác thực email thành công. Bạn đã có thể đăng nhập vào hệ thống."));
    }
}
