using IdentityDbContext = HomeMatch.Modules.Identity.Infrastructure.Persistence.IdentityDbContext;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

public sealed class SqlIdentityUserStore(IdentityDbContext database)
    : UserStore<IdentityAccount, IdentityRole<long>, IdentityDbContext, long>(database)
{
    private static string CanonicalRole(string role) => AccountRoles.All.Single(value => value.Equals(role, StringComparison.OrdinalIgnoreCase));
    public override Task AddToRoleAsync(IdentityAccount user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.Role = CanonicalRole(normalizedRoleName);
        return Task.CompletedTask;
    }
    public override Task<IList<string>> GetRolesAsync(IdentityAccount user, CancellationToken cancellationToken = default)
        => Task.FromResult<IList<string>>(new List<string> { user.Role });
    public override Task<bool> IsInRoleAsync(IdentityAccount user, string normalizedRoleName, CancellationToken cancellationToken = default)
        => Task.FromResult(user.Role.Equals(normalizedRoleName, StringComparison.OrdinalIgnoreCase));
    public override async Task<IList<IdentityAccount>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var role = CanonicalRole(normalizedRoleName);
        return await Context.Users.Where(user => user.Role == role).ToListAsync(cancellationToken);
    }
    public override Task RemoveFromRoleAsync(IdentityAccount user, string normalizedRoleName, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("An account must retain its business role.");
}
