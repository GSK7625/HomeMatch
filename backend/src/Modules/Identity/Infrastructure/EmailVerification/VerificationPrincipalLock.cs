using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

/// <summary>Called inside a transaction, before challenge/user quota locks.</summary>
internal static class VerificationPrincipalLock
{
    public static async Task<IdentityAccount?> AcquireAsync(IdentityDbContext db, VerificationSubject subject,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        var account = (await db.Users.FromSqlInterpolated($"""
            SELECT * FROM identity."Users" WHERE "Id" = {subject.UserId} FOR UPDATE
            """).ToListAsync(cancellationToken)).SingleOrDefault();
        var session = (await db.Sessions.FromSqlInterpolated($"""
            SELECT * FROM identity."Sessions" WHERE "Id" = {subject.SessionId} AND "UserId" = {subject.UserId} FOR UPDATE
            """).ToListAsync(cancellationToken)).SingleOrDefault();
        if (account is null || session is null || account.Status != AccountStatus.Active
            || !session.IsActiveAt(clock.GetUtcNow()) || session.SecurityVersion != account.SecurityVersion
            || !await db.Profiles.AnyAsync(value => value.UserId == subject.UserId, cancellationToken)
            || !await (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id
                       where membership.UserId == subject.UserId && role.Name == subject.Role
                       select membership.UserId).AnyAsync(cancellationToken))
            return null;
        return account;
    }
}
