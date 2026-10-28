using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

public sealed class DatabaseAccessIdentityReader(IdentityDbContext database, TimeProvider clock) : IAccessIdentityReader
{
    public async Task<AuthenticatedIdentity?> FindActiveAsync(AccessTokenIdentity identity, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        // One query/snapshot for account, role, profile and session; no per-request result cache.
        var row = await (
            from session in database.Sessions.AsNoTracking()
            join account in database.Users on session.UserId equals account.Id
            join membership in database.UserRoles on account.Id equals membership.UserId
            join role in database.Roles on membership.RoleId equals role.Id
            join profile in database.Profiles on account.Id equals profile.UserId
            where session.Id == identity.SessionId && account.Id == identity.UserId
                && account.Status == AccountStatus.Active
                && account.SecurityVersion == identity.SecurityVersion
                && session.SecurityVersion == identity.SecurityVersion
                && session.RevokedAt == null && session.CreatedAt <= now && session.ExpiresAt > now
                && identity.ExpiresAt <= session.ExpiresAt && role.Name == identity.Role
            select new
            {
                account.Id, account.Email, account.EmailConfirmed, role.Name, profile.FullName,
                profile.PhoneNumber, account.CreatedAt, profile.UpdatedAt
            }).SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : new AuthenticatedIdentity(row.Id, identity.SessionId, identity.Role,
            new CurrentUserProfile(row.Id, row.Email!, row.EmailConfirmed, row.Name!, row.FullName,
                row.PhoneNumber, row.CreatedAt.UtcDateTime, row.UpdatedAt.UtcDateTime));
    }
}
