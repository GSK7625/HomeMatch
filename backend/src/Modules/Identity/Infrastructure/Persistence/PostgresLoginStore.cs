using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

public sealed class PostgresLoginStore(IdentityDbContext database, UserManager<IdentityAccount> users,
    IPasswordHasher<IdentityAccount> hasher, LoginDummyCredential dummy, IAccessTokenIssuer tokens,
    IOptions<IdentitySessionOptions> sessions, TimeProvider clock, ILogger<PostgresLoginStore> logger) : ILoginStore
{
    public async Task<LoginResult> AuthenticateAndStartSessionAsync(ValidatedLogin login, string? previousRefreshSecret,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            // The account lock serializes password checks, rehashing and lockout updates across requests.
            // Always lock account before touching session rows; keep this order in refresh/logout tasks.
            var matches = await database.Users.FromSqlInterpolated($"""
                SELECT * FROM identity."Users" WHERE "NormalizedEmail" = {login.NormalizedEmail} FOR UPDATE
                """).ToListAsync(cancellationToken);
            var account = matches.SingleOrDefault();
            if (account is null || await users.IsLockedOutAsync(account))
            {
                dummy.Verify(hasher, login.Password);
                return new(LoginOutcome.InvalidCredentials);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!await users.CheckPasswordAsync(account, login.Password))
            {
                if (account.LockoutEnabled)
                {
                    EnsureSuccess(await users.AccessFailedAsync(account));
                    await transaction.CommitAsync(cancellationToken);
                }
                return new(LoginOutcome.InvalidCredentials);
            }
            if (account.Status != AccountStatus.Active) return new(LoginOutcome.AccountSuspended);

            var role = (await users.GetRolesAsync(account)).SingleOrDefault();
            if (role is null || !AccountRoles.All.Contains(role)
                || !await database.Profiles.AnyAsync(profile => profile.UserId == account.Id, cancellationToken))
                return new(LoginOutcome.Unavailable);

            var now = PostgresTimestamp.UtcNow(clock);
            if (account.AccessFailedCount != 0 || account.LockoutEnd is not null)
            {
                account.AccessFailedCount = 0;
                account.LockoutEnd = null;
                account.UpdatedAt = now;
                EnsureSuccess(await users.UpdateAsync(account));
            }

            var lifetime = login.RememberMe ? TimeSpan.FromDays(sessions.Value.RememberLifetimeDays)
                : TimeSpan.FromHours(sessions.Value.NormalLifetimeHours);
            var session = new AccountSession(account.Id, account.SecurityVersion, login.RememberMe, now, now.Add(lifetime));
            var refresh = RefreshSecrets.Create();
            database.Sessions.Add(session);
            database.RefreshCredentials.Add(new RefreshCredential(session.Id, refresh.Hash, now, session.ExpiresAt));

            // Only a valid existing cookie can replace a session, including when changing accounts in one browser.
            // No cookie is read by the endpoint until Origin/CSRF validation has succeeded.
            var oldHash = RefreshSecrets.TryHash(previousRefreshSecret);
            if (oldHash is not null)
                await RevokePreviousSessionAsync(oldHash, now, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);
            var issued = tokens.Issue(new AccessTokenSubject(account.Id, session.Id, role, account.SecurityVersion, session.ExpiresAt));
            await transaction.CommitAsync(cancellationToken);
            return new(LoginOutcome.Succeeded, issued, refresh.Raw, login.RememberMe);
        }
        catch (Exception exception) when (IdentityDatabaseFailure.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity login database is unavailable.");
            return new(LoginOutcome.Unavailable);
        }
    }

    private async Task RevokePreviousSessionAsync(string hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var previous = await (
            from credential in database.RefreshCredentials
            join session in database.Sessions on credential.SessionId equals session.Id
            join account in database.Users on session.UserId equals account.Id
            where credential.TokenHash == hash && credential.UsedAt == null && credential.RevokedAt == null
                && credential.CreatedAt <= now && credential.ExpiresAt > now
                && session.RevokedAt == null && session.CreatedAt <= now && session.ExpiresAt > now
                && account.Status == AccountStatus.Active && account.SecurityVersion == session.SecurityVersion
            select (Guid?)session.Id).SingleOrDefaultAsync(cancellationToken);
        if (previous is not null)
            await database.Sessions.Where(session => session.Id == previous && session.RevokedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, now), cancellationToken);
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Identity credential state update failed.");
    }
}
