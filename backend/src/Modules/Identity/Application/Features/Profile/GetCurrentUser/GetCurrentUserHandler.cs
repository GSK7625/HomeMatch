using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Profile.GetCurrentUser;

public class GetCurrentUserHandler
{
    private readonly IIdentityAccountService _accountService;
    private readonly IUserProfileRepository _profileRepository;

    public GetCurrentUserHandler(
        IIdentityAccountService accountService,
        IUserProfileRepository profileRepository)
    {
        _accountService = accountService;
        _profileRepository = profileRepository;
    }

    public async Task<Result<CurrentUserResult>> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken = default)
    {
        if (query.UserId <= 0)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                "Identity.AuthenticationRequired",
                "Chưa xác thực người dùng."));
        }

        var user = await _accountService.GetUserDetailsByIdAsync(query.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                "Identity.AuthenticationRequired",
                "Tài khoản không tồn tại."));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                IdentityErrors.AccountSuspended,
                "Tài khoản đã bị tạm khóa."));
        }

        // Kiểm tra tính hợp lệ của phiên so với SecurityVersion hiện hành
        if (query.TokenSecurityVersion.HasValue && query.TokenSecurityVersion.Value < user.SecurityVersion)
        {
            return Result.Failure<CurrentUserResult>(new Error(
                IdentityErrors.SessionInvalid,
                "Phiên đăng nhập đã bị vô hiệu hóa do tài khoản đã đăng xuất tất cả phiên hoặc đổi mật khẩu."));
        }

        var profile = await _profileRepository.GetByUserIdAsync(user.UserId, cancellationToken);

        return Result.Success(new CurrentUserResult(
            Id: user.UserId,
            Email: user.Email,
            EmailVerified: user.EmailConfirmed,
            Role: user.Role,
            FullName: profile?.FullName ?? user.Email,
            PhoneNumber: user.PhoneNumber,
            AvatarUrl: profile?.AvatarUrl,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt));
    }
}
