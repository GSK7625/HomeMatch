using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Admin.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    string? Search,
    string? Role,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed class GetAdminUsersHandler(IAdminUserService adminUserService)
{
    public async Task<Result<PagedAdminUsersResult>> HandleAsync(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await adminUserService.GetUsersAsync(
            query.Search,
            query.Role,
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);
        return Result.Success(result);
    }
}
