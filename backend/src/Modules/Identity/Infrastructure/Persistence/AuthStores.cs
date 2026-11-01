using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Application.Logout;
using HomeMatch.Modules.Identity.Application.Refresh;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

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

            var now = PostgresTimestampHelper.UtcNow(clock);
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
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
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

public sealed class PostgresRegistrationStore(IdentityDbContext database, UserManager<IdentityAccount> users,
    TimeProvider clock, ILogger<PostgresRegistrationStore> logger) : IRegistrationStore
{
    public async Task<RegistrationOutcome> CreateIfAbsentAsync(ValidatedRegistration registration, CancellationToken cancellationToken)
    {
        if (!AccountRoles.CanSelfRegister(registration.Role))
            throw new ArgumentException("Public registration cannot provision this role.", nameof(registration));

        try
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            if (users.NormalizeEmail(registration.Email) != registration.NormalizedEmail)
                throw new InvalidOperationException("Email normalization is inconsistent.");
            var account = new IdentityAccount(registration.Email, clock.GetUtcNow()) { Role = registration.Role };
            // CreateAsync(user, password) validates/hashes before duplicate user lookup.
            // Keep both new and existing email branches on this path; no existence fast-path.
            cancellationToken.ThrowIfCancellationRequested();
            var created = await users.CreateAsync(account, registration.Password);
            cancellationToken.ThrowIfCancellationRequested();
            if (!created.Succeeded)
            {
                if (created.Errors.Any() && created.Errors.All(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
                    return RegistrationOutcome.Accepted; // transaction disposed/rolled back without modifying the old account
                throw new InvalidOperationException("Identity account provisioning failed.");
            }


            database.Profiles.Add(new UserProfile(account.Id, registration.FullName, account.CreatedAt));
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RegistrationOutcome.Accepted;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation, SchemaName: "identity", TableName: "users" } postgres
               && postgres.ConstraintName is "UX_Users_NormalizedEmail" or "UserNameIndex")
        {
            // The unique index wins a race even if both UserManager lookups saw no account.
            database.ChangeTracker.Clear();
            return RegistrationOutcome.Accepted;
        }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity registration database is unavailable.");
            return RegistrationOutcome.Unavailable;
        }
    }
}

public sealed class PostgresRefreshSessionStore(IdentityDbContext database, IAccessTokenIssuer tokens,
    IRefreshRequestQuota quota, TimeProvider clock, ILogger<PostgresRefreshSessionStore> logger) : IRefreshSessionStore
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
                SELECT * FROM identity."users" WHERE "id" = {located.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var session = (await database.Sessions.FromSqlInterpolated($"""
                SELECT * FROM identity."sessions"
                WHERE "id" = {located.SessionId} AND "user_id" = {located.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            // The account lock serializes token rotation; EF joins canonical token rows with refresh metadata.
            var credential = await database.RefreshCredentials.SingleOrDefaultAsync(
                value => value.Id == located.CredentialId && value.SessionId == located.SessionId && value.TokenHash == hash,
                cancellationToken);
            if (account is null || session is null || credential is null) return new(RefreshOutcome.InvalidSession);

            var now = PostgresTimestampHelper.UtcNow(clock);
            if (credential.UsedAt is not null)
            {
                // A duplicate request is replay, even if another request just rotated successfully.
                await SessionRevocationHelper.RevokeFamilyAsync(database, session, now, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(RefreshOutcome.InvalidSession);
            }
            if (account.Status != AccountStatus.Active || account.SecurityVersion != session.SecurityVersion
                || !session.IsActiveAt(now) || !credential.IsUsableAt(now)
                || credential.ExpiresAt > session.ExpiresAt)
                return new(RefreshOutcome.InvalidSession);

            var role = account.Role;
            if (role is null || !AccountRoles.All.Contains(role)
                || !await database.Profiles.AnyAsync(profile => profile.UserId == account.Id, cancellationToken))
                return new(RefreshOutcome.InvalidSession);

            var fresh = RefreshSecrets.Create();
            var replacement = new RefreshCredential(session.Id, fresh.Hash, now, credential.ExpiresAt, account.Id);
            credential.Consume(now);
            // Release the partial unique index slot before inserting the successor.
            // This save remains inside the transaction; rollback restores the original credential.
            await database.SaveChangesAsync(cancellationToken);
            database.RefreshCredentials.Add(replacement);
            await database.SaveChangesAsync(cancellationToken);
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
            return new(RefreshOutcome.Succeeded, issued, fresh.Raw, RefreshExpiresAt: replacement.ExpiresAt.UtcDateTime);
        }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity refresh database is unavailable.");
            return new(RefreshOutcome.Unavailable);
        }
    }

    private sealed record CredentialLocation(long CredentialId, Guid SessionId, long UserId);
}

public sealed class PostgresLogoutSessionStore(IdentityDbContext database, TimeProvider clock,
    ILogger<PostgresLogoutSessionStore> logger) : ILogoutSessionStore
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
                SELECT * FROM identity."users" WHERE "id" = {location.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var session = (await database.Sessions.FromSqlInterpolated($"""
                SELECT * FROM identity."sessions" WHERE "id" = {location.SessionId} AND "user_id" = {location.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            if (owner is null || session is null) return new(LogoutOutcome.Succeeded, true);
            await SessionRevocationHelper.RevokeFamilyAsync(database, session, PostgresTimestampHelper.UtcNow(clock), cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(LogoutOutcome.Succeeded, true);
        }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
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
                SELECT * FROM identity."users" WHERE "id" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var session = (await database.Sessions.FromSqlInterpolated($"""
                SELECT * FROM identity."sessions" WHERE "id" = {subject.SessionId} AND "user_id" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var now = PostgresTimestampHelper.UtcNow(clock);
            // Authentication may have completed before another request revoked this principal.
            // Recheck under the account lock so two concurrent logout-all requests cannot both succeed.
            if (account is null || session is null || account.Status != AccountStatus.Active
                || !session.IsActiveAt(now) || session.SecurityVersion != account.SecurityVersion
                || !await database.Profiles.AnyAsync(profile => profile.UserId == subject.UserId, cancellationToken)
                || account.Role != subject.Role)
                return new(LogoutOutcome.InvalidAuthentication);

            var cookieHash = RefreshSecrets.TryHash(rawRefreshSecret);
            var clearCookie = cookieHash is not null && await (
                from credential in database.RefreshCredentials
                join parent in database.Sessions on credential.SessionId equals parent.Id
                where credential.TokenHash == cookieHash && parent.UserId == subject.UserId
                select credential.Id).AnyAsync(cancellationToken);
            await SessionRevocationHelper.RevokeAccountAsync(database, subject.UserId, now, cancellationToken);
            account.SecurityVersion = checked(account.SecurityVersion + 1);
            account.UpdatedAt = now;
            account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(LogoutOutcome.Succeeded, clearCookie);
        }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity logout-all database is unavailable.");
            return new(LogoutOutcome.Unavailable);
        }
    }

    private sealed record SessionLocation(Guid SessionId, long UserId);
}
