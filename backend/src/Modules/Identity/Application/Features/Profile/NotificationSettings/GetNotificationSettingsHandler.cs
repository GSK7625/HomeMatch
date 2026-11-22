using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Profile.NotificationSettings;

public class GetNotificationSettingsHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IUserProfileRepository _profileRepository;

    public GetNotificationSettingsHandler(
        IIdentityAccountService accountService,
        IUserProfileRepository profileRepository)
    {
        _accountService = accountService;
        _profileRepository = profileRepository;
    }

    public async Task<Result<NotificationSettingsResult>> HandleAsync(
        GetNotificationSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId <= 0)
        {
            return Result.Failure<NotificationSettingsResult>(new Error(
                IdentityErrors.AuthenticationRequired,
                "Chưa xác thực người dùng."));
        }

        var user = await _accountService.GetUserDetailsByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<NotificationSettingsResult>(new Error(
                IdentityErrors.AuthenticationRequired,
                "Tài khoản không tồn tại."));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<NotificationSettingsResult>(new Error(
                IdentityErrors.AccountSuspended,
                "Tài khoản đã bị tạm khóa."));
        }

        if (query.TokenSecurityVersion.HasValue && query.TokenSecurityVersion.Value < user.SecurityVersion)
        {
            return Result.Failure<NotificationSettingsResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phiên đăng nhập đã bị vô hiệu hóa. Vui lòng đăng nhập lại."));
        }

        var profile = await _profileRepository.GetByUserIdAsync(user.UserId, cancellationToken);

        return Result.Success(new NotificationSettingsResult(
            NotifyPush: profile?.NotifyPush ?? true,
            UpdatedAt: profile?.UpdatedAt ?? user.UpdatedAt));
    }
}
