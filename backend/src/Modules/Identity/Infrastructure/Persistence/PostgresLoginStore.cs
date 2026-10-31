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
            // Serialize credential checks with concurrent account/session changes.
            var matches = await database.Users.FromSqlInterpolated($"""
                SELECT * FROM identity."users" WHERE "normalized_email" = {login.NormalizedEmail} FOR UPDATE
                """).ToListAsync(cancellationToken);
            var account = matches.SingleOrDefault();
            if (account is null)
            {
                dummy.Verify(hasher, login.Password);
                return new(LoginOutcome.InvalidCredentials);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!await users.CheckPasswordAsync(account, login.Password))
            {
                return new(LoginOutcome.InvalidCredentials);
            }
            if (account.Status != AccountStatus.Active) return new(LoginOutcome.AccountSuspended);
            if (!account.EmailConfirmed) return new(LoginOutcome.EmailNotVerified);

            var role = (await users.GetRolesAsync(account)).SingleOrDefault();
            if (role is null || !AccountRoles.All.Contains(role)
                || !await database.Profiles.AnyAsync(profile => profile.UserId == account.Id, cancellationToken))
                return new(LoginOutcome.Unavailable);

            var now = PostgresTimestamp.UtcNow(clock);
            var session = new AccountSession(account.Id, account.SecurityVersion, now,
                now.AddHours(sessions.Value.NormalLifetimeHours));
            var refresh = RefreshSecrets.Create();
            database.Sessions.Add(session);
            database.RefreshCredentials.Add(new RefreshCredential(session.Id, refresh.Hash, now, session.ExpiresAt, account.Id));

            // Only a valid existing cookie can replace a session, including when changing accounts in one browser.
            // No cookie is read by the endpoint until Origin/CSRF validation has succeeded.
            var oldHash = RefreshSecrets.TryHash(previousRefreshSecret);
            if (oldHash is not null)
                await RevokePreviousSessionAsync(oldHash, now, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);
            var issued = tokens.Issue(new AccessTokenSubject(account.Id, session.Id, role, account.SecurityVersion, session.ExpiresAt));
            await transaction.CommitAsync(cancellationToken);
            return new(LoginOutcome.Succeeded, issued, refresh.Raw);
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
}
