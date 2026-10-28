using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Refresh;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

public sealed class PostgresRefreshSessionService(IdentityDbContext database, IAccessTokenIssuer tokens,
    IRefreshRequestQuota quota, TimeProvider clock, ILogger<PostgresRefreshSessionService> logger) : IRefreshSessionService
{
    public async Task<RefreshResult> RefreshAsync(string? rawSecret, string clientAddress, CancellationToken cancellationToken)
    {
        try
        {
            var hash = RefreshSecrets.TryHash(rawSecret);
            // Read only routing metadata before admission. Do not cache validity before taking locks.
            var located = hash is null ? null : await (
                from entry in database.RefreshCredentials.AsNoTracking()
                join ownerSession in database.Sessions on entry.SessionId equals ownerSession.Id
                where entry.TokenHash == hash
                select new CredentialLocation(entry.Id, ownerSession.Id, ownerSession.UserId)).SingleOrDefaultAsync(cancellationToken);
            var retry = quota.TryAcquire(clientAddress, located?.SessionId);
            if (retry is not null) return new(RefreshOutcome.RateLimited, RetryAfter: retry);
            if (located is null) return new(RefreshOutcome.InvalidSession);

            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            // Match login's account -> session -> credential lock order.
            var account = (await database.Users.FromSqlInterpolated($"""
                SELECT * FROM identity."Users" WHERE "Id" = {located.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var session = (await database.Sessions.FromSqlInterpolated($"""
                SELECT * FROM identity."Sessions"
                WHERE "Id" = {located.SessionId} AND "UserId" = {located.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var credential = (await database.RefreshCredentials.FromSqlInterpolated($"""
                SELECT * FROM identity."RefreshCredentials"
                WHERE "Id" = {located.CredentialId} AND "SessionId" = {located.SessionId} AND "TokenHash" = {hash} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            if (account is null || session is null || credential is null) return new(RefreshOutcome.InvalidSession);

            var now = PostgresTimestamp.UtcNow(clock);
            if (credential.UsedAt is not null)
            {
                // A duplicate request is replay, even if another request just rotated successfully.
                await PostgresSessionRevocation.RevokeFamilyAsync(database, session, now, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(RefreshOutcome.InvalidSession);
            }
            if (account.Status != AccountStatus.Active || account.SecurityVersion != session.SecurityVersion
                || !session.IsActiveAt(now) || !credential.IsUsableAt(now)
                || credential.ExpiresAt > session.ExpiresAt)
                return new(RefreshOutcome.InvalidSession);

            var role = await (
                from membership in database.UserRoles
                join value in database.Roles on membership.RoleId equals value.Id
                where membership.UserId == account.Id
                select value.Name).SingleOrDefaultAsync(cancellationToken);
            if (role is null || !AccountRoles.All.Contains(role)
                || !await database.Profiles.AnyAsync(profile => profile.UserId == account.Id, cancellationToken))
                return new(RefreshOutcome.InvalidSession);

            var fresh = RefreshSecrets.Create();
            var replacement = new RefreshCredential(session.Id, fresh.Hash, now, credential.ExpiresAt);
            credential.Consume(now);
            // Release the partial unique index slot before inserting the successor.
            // This save remains inside the transaction; rollback restores the original credential.
            await database.SaveChangesAsync(cancellationToken);
            database.RefreshCredentials.Add(replacement);
            credential.LinkReplacement(replacement);
            await database.SaveChangesAsync(cancellationToken);
            IssuedAccessToken issued;
            try
            {
                issued = tokens.Issue(new AccessTokenSubject(account.Id, session.Id, role, session.SecurityVersion, session.ExpiresAt));
            }
            catch (AccessTokenSessionExpiredException)
            {
                // Expiry may be reached while waiting on SQL. Returning disposes/rolls back the transaction.
                return new(RefreshOutcome.InvalidSession);
            }
            await transaction.CommitAsync(cancellationToken);
            return new(RefreshOutcome.Succeeded, issued, fresh.Raw, session.RememberMe, RefreshExpiresAt: replacement.ExpiresAt.UtcDateTime);
        }
        catch (Exception exception) when (IdentityDatabaseFailure.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity refresh database is unavailable.");
            return new(RefreshOutcome.Unavailable);
        }
    }

    private sealed record CredentialLocation(Guid CredentialId, Guid SessionId, Guid UserId);
}
