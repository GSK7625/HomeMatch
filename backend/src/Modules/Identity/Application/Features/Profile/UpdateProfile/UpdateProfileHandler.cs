using HomeMatch.Modules.Identity.Application.Features.Profile.GetCurrentUser;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Profile.UpdateProfile;

public class UpdateProfileHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IUserProfileRepository _profileRepository;

    public UpdateProfileHandler(
        IIdentityAccountService accountService,
        IUserProfileRepository profileRepository)
    {
        _accountService = accountService;
        _profileRepository = profileRepository;
    }

    public async Task<Result<CurrentUserResult>> HandleAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = UpdateProfileValidator.Validate(command);
        if (validationError is not null)
        {
            return Result.Failure<CurrentUserResult>(validationError);
        }

        var user = await _accountService.GetUserDetailsByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                IdentityErrors.AuthenticationRequired,
                "Tài khoản không tồn tại."));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                IdentityErrors.AccountSuspended,
                "Tài khoản đã bị tạm khóa."));
        }

        if (command.TokenSecurityVersion.HasValue && command.TokenSecurityVersion.Value < user.SecurityVersion)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phiên đăng nhập đã bị vô hiệu hóa. Vui lòng đăng nhập lại."));
        }

        var normalizedPhone = string.IsNullOrWhiteSpace(command.PhoneNumber)
            ? null
            : command.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");

        var phoneResult = await _accountService.UpdatePhoneNumberAsync(user.UserId, normalizedPhone, cancellationToken);
        if (!phoneResult.Succeeded)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                IdentityErrors.ValidationFailed,
                "Không thể cập nhật số điện thoại. Vui lòng thử lại."));
        }

        var now = DateTimeOffset.UtcNow;
        var normalizedGender = string.IsNullOrWhiteSpace(command.Gender)
            ? null
            : command.Gender.Trim().ToUpperInvariant();

        var profile = await _profileRepository.GetByUserIdAsync(user.UserId, cancellationToken);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = user.UserId,
                FullName = command.FullName.Trim(),
                DateOfBirth = command.DateOfBirth,
                Gender = normalizedGender,
                NotifyEmail = true,
                NotifySms = false,
                NotifyPush = true,
                UpdatedAt = now
            };
            await _profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.FullName = command.FullName.Trim();
            profile.DateOfBirth = command.DateOfBirth;
            profile.Gender = normalizedGender;
            profile.UpdatedAt = now;
        }

        await _profileRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new CurrentUserResult(
            Id: user.UserId,
            Email: user.Email,
            EmailVerified: user.EmailConfirmed,
            Role: user.Role,
            FullName: profile.FullName,
            PhoneNumber: normalizedPhone,
            AvatarUrl: profile.AvatarUrl,
            DateOfBirth: profile.DateOfBirth,
            Gender: profile.Gender,
            NotifyPush: profile.NotifyPush,
            CreatedAt: user.CreatedAt,
            UpdatedAt: profile.UpdatedAt));
    }
}
