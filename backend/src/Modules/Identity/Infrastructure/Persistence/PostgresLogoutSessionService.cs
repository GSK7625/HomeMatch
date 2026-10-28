using HomeMatch.Modules.Identity.Application.Logout;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

public sealed class PostgresLogoutSessionService(IdentityDbContext database, TimeProvider clock,
    ILogger<PostgresLogoutSessionService> logger) : ILogoutSessionService
{
    public async Task<LogoutResult> LogoutCurrentAsync(string? rawRefreshSecret, CancellationToken cancellationToken)
    {
        var hash = RefreshSecrets.TryHash(rawRefreshSecret);
        if (hash is null) return new(LogoutOutcome.Succeeded, true);
        try
        {
            // History, expired and already-used credentials still identify the same logout family.
            var location = await (
                from credential in database.RefreshCredentials.AsNoTracking()
                join parent in database.Sessions on credential.SessionId equals parent.Id
                where credential.TokenHash == hash
                select new SessionLocation(parent.Id, parent.UserId)).SingleOrDefaultAsync(cancellationToken);
            if (location is null) return new(LogoutOutcome.Succeeded, true);

            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            var owner = (await database.Users.FromSqlInterpolated($"""
                SELECT * FROM identity."Users" WHERE "Id" = {location.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var session = (await database.Sessions.FromSqlInterpolated($"""
                SELECT * FROM identity."Sessions" WHERE "Id" = {location.SessionId} AND "UserId" = {location.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            if (owner is null || session is null) return new(LogoutOutcome.Succeeded, true);
            await PostgresSessionRevocation.RevokeFamilyAsync(database, session, PostgresTimestamp.UtcNow(clock), cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(LogoutOutcome.Succeeded, true);
        }
        catch (Exception exception) when (IdentityDatabaseFailure.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity logout database is unavailable.");
            return new(LogoutOutcome.Unavailable);
        }
    }

    public async Task<LogoutResult> LogoutAllAsync(LogoutAllSubject subject, string? rawRefreshSecret,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            var account = (await database.Users.FromSqlInterpolated($"""
                SELECT * FROM identity."Users" WHERE "Id" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var session = (await database.Sessions.FromSqlInterpolated($"""
                SELECT * FROM identity."Sessions" WHERE "Id" = {subject.SessionId} AND "UserId" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var now = PostgresTimestamp.UtcNow(clock);
            // Authentication may have completed before another request revoked this principal.
            // Recheck under the account lock so two concurrent logout-all requests cannot both succeed.
            if (account is null || session is null || account.Status != AccountStatus.Active
                || !session.IsActiveAt(now) || session.SecurityVersion != account.SecurityVersion
                || !await database.Profiles.AnyAsync(profile => profile.UserId == subject.UserId, cancellationToken)
                || !await (from membership in database.UserRoles
                           join role in database.Roles on membership.RoleId equals role.Id
                           where membership.UserId == subject.UserId && role.Name == subject.Role
                           select membership.UserId).AnyAsync(cancellationToken))
                return new(LogoutOutcome.InvalidAuthentication);

            var cookieHash = RefreshSecrets.TryHash(rawRefreshSecret);
            var clearCookie = cookieHash is not null && await (
                from credential in database.RefreshCredentials
                join parent in database.Sessions on credential.SessionId equals parent.Id
                where credential.TokenHash == cookieHash && parent.UserId == subject.UserId
                select credential.Id).AnyAsync(cancellationToken);
            await PostgresSessionRevocation.RevokeAccountAsync(database, subject.UserId, now, cancellationToken);
            account.SecurityVersion = checked(account.SecurityVersion + 1);
            account.UpdatedAt = now;
            account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(LogoutOutcome.Succeeded, clearCookie);
        }
        catch (Exception exception) when (IdentityDatabaseFailure.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity logout-all database is unavailable.");
            return new(LogoutOutcome.Unavailable);
        }
    }

    private sealed record SessionLocation(Guid SessionId, Guid UserId);
}
