using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Admin.SetUserLockStatus;

public sealed record SetUserLockStatusCommand(long AdminId, long TargetUserId, bool Lock);

public sealed class SetUserLockStatusHandler(IAdminUserService adminUserService)
{
    public async Task<Result<AdminUserItemDto>> HandleAsync(
        SetUserLockStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TargetUserId <= 0)
        {
            return Result.Failure<AdminUserItemDto>(new Error("Identity.InvalidUserId", "Mã tài khoản không hợp lệ."));
        }

        if (command.Lock && command.AdminId == command.TargetUserId)
        {
            return Result.Failure<AdminUserItemDto>(new Error("Identity.CannotLockSelf", "Bạn không thể tự khóa tài khoản của chính mình."));
        }

        var nextStatus = command.Lock ? UserStatus.Suspended : UserStatus.Active;
        return await adminUserService.SetUserStatusAsync(command.TargetUserId, nextStatus, cancellationToken);
    }
}
