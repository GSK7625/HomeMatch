using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Profile.NotificationSettings;

public class UpdateNotificationSettingsHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IUserProfileRepository _profileRepository;

    public UpdateNotificationSettingsHandler(
        IIdentityAccountService accountService,
        IUserProfileRepository profileRepository)
    {
        _accountService = accountService;
        _profileRepository = profileRepository;
    }

    public async Task<Result<NotificationSettingsResult>> HandleAsync(
        UpdateNotificationSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId <= 0)
        {
            return Result.Failure<NotificationSettingsResult>(new Error(
                IdentityErrors.AuthenticationRequired,
                "Chưa xác thực người dùng."));
        }

        var user = await _accountService.GetUserDetailsByIdAsync(command.UserId, cancellationToken);
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

        if (command.TokenSecurityVersion.HasValue && command.TokenSecurityVersion.Value < user.SecurityVersion)
        {
            return Result.Failure<NotificationSettingsResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phiên đăng nhập đã bị vô hiệu hóa. Vui lòng đăng nhập lại."));
        }

        var now = DateTimeOffset.UtcNow;
        var profile = await _profileRepository.GetByUserIdAsync(user.UserId, cancellationToken);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = user.UserId,
                FullName = user.Email,
                NotifyEmail = true,
                NotifySms = false,
                NotifyPush = command.NotifyPush,
                UpdatedAt = now
            };
            await _profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.NotifyPush = command.NotifyPush;
            profile.UpdatedAt = now;
        }

        await _profileRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new NotificationSettingsResult(
            NotifyPush: profile.NotifyPush,
            UpdatedAt: profile.UpdatedAt));
    }
}
