using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Interfaces;

public sealed record AdminUserItemDto(
    long Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    string Status,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PagedAdminUsersResult(
    IReadOnlyList<AdminUserItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface IAdminUserService
{
    Task<PagedAdminUsersResult> GetUsersAsync(
        string? search,
        string? role,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserItemDto>> SetUserStatusAsync(
        long targetUserId,
        UserStatus newStatus,
        CancellationToken cancellationToken = default);
}
