using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Identity;

internal sealed class AdminUserService(
    IdentityDbContext db,
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokenRepository) : IAdminUserService, IAdminIdentityStatsReader
{
    public async Task<PagedAdminUsersResult> GetUsersAsync(
        string? search,
        string? role,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var trimmedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var trimmedRole = string.IsNullOrWhiteSpace(role) ? null : role.Trim();

        UserStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<UserStatus>(status.Trim(), ignoreCase: true, out var s))
            {
                parsedStatus = s;
            }
            else if (string.Equals(status.Trim(), "LOCKED", StringComparison.OrdinalIgnoreCase))
            {
                parsedStatus = UserStatus.Suspended;
            }
        }

        var query =
            from user in db.Users.AsNoTracking()
            join profile in db.UserProfiles.AsNoTracking() on user.Id equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId into userRoleGroup
            from userRole in userRoleGroup.DefaultIfEmpty()
            join roleEntity in db.Roles.AsNoTracking() on userRole.RoleId equals roleEntity.Id into roleGroup
            from roleEntity in roleGroup.DefaultIfEmpty()
            select new
            {
                user.Id,
                FullName = profile != null && !string.IsNullOrEmpty(profile.FullName)
                    ? profile.FullName
                    : (user.Email ?? string.Empty),
                Email = user.Email ?? string.Empty,
                user.PhoneNumber,
                Role = roleEntity != null && !string.IsNullOrEmpty(roleEntity.Name)
                    ? roleEntity.Name
                    : AccountRoles.Tenant,
                user.Status,
                user.EmailConfirmed,
                user.CreatedAt,
                user.UpdatedAt
            };

        if (trimmedRole is not null)
        {
            query = query.Where(u => u.Role == trimmedRole);
        }

        if (parsedStatus.HasValue)
        {
            query = query.Where(u => u.Status == parsedStatus.Value);
        }

        if (trimmedSearch is not null)
        {
            var pattern = "%" + trimmedSearch.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern, "\\")
                || EF.Functions.ILike(u.FullName, pattern, "\\")
                || (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, pattern, "\\")));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(u => new AdminUserItemDto(
            Id: u.Id,
            FullName: u.FullName,
            Email: u.Email,
            PhoneNumber: u.PhoneNumber,
            Role: u.Role,
            Status: u.Status.ToString(),
            EmailConfirmed: u.EmailConfirmed,
            CreatedAt: u.CreatedAt,
            UpdatedAt: u.UpdatedAt)).ToList();

        return new PagedAdminUsersResult(items, safePage, safePageSize, totalCount);
    }

    public async Task<Result<AdminUserItemDto>> SetUserStatusAsync(
        long targetUserId,
        UserStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(targetUserId.ToString());
        if (user is null)
        {
            return Result.Failure<AdminUserItemDto>(new Error("Identity.UserNotFound", "Không tìm thấy tài khoản người dùng."));
        }

        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? AccountRoles.Tenant;
        if (string.Equals(primaryRole, AccountRoles.Admin, StringComparison.OrdinalIgnoreCase)
            && newStatus == UserStatus.Suspended)
        {
            return Result.Failure<AdminUserItemDto>(new Error("Identity.CannotLockAdmin", "Không thể khóa tài khoản Quản trị viên."));
        }

        user.Status = newStatus;
        user.SecurityVersion += 1;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Failure<AdminUserItemDto>(new Error(
                "Identity.UpdateFailed",
                string.Join("; ", updateResult.Errors.Select(e => e.Description))));
        }

        if (newStatus == UserStatus.Suspended)
        {
            await refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, cancellationToken);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        return Result.Success(new AdminUserItemDto(
            Id: user.Id,
            FullName: profile?.FullName ?? user.Email ?? string.Empty,
            Email: user.Email ?? string.Empty,
            PhoneNumber: user.PhoneNumber,
            Role: primaryRole,
            Status: user.Status.ToString(),
            EmailConfirmed: user.EmailConfirmed,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt));
    }

    public async Task<AdminIdentityStats> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken = default)
    {
        var usersWithRoles = await (
            from user in db.Users.AsNoTracking()
            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId into urGroup
            from userRole in urGroup.DefaultIfEmpty()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id into rGroup
            from role in rGroup.DefaultIfEmpty()
            select new
            {
                user.Status,
                user.CreatedAt,
                Role = role != null ? role.Name : null
            }).ToListAsync(cancellationToken);

        var totalUsers = usersWithRoles.Count;
        var totalTenants = usersWithRoles.Count(u => string.Equals(u.Role, AccountRoles.Tenant, StringComparison.OrdinalIgnoreCase));
        var totalLandlords = usersWithRoles.Count(u => string.Equals(u.Role, AccountRoles.Landlord, StringComparison.OrdinalIgnoreCase));
        var suspendedUsers = usersWithRoles.Count(u => u.Status == UserStatus.Suspended);
        var newUsersInPeriod = fromUtc.HasValue
            ? usersWithRoles.Count(u => u.CreatedAt >= fromUtc.Value)
            : totalUsers;

        return new AdminIdentityStats(totalUsers, totalTenants, totalLandlords, suspendedUsers, newUsersInPeriod);
    }
}
