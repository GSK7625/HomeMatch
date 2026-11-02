using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

public sealed partial class PostgresEmailVerificationStore(IdentityDbContext db, EmailVerificationCodeProtector protector,
    IOptions<EmailVerificationOptions> options, TimeProvider clock, ILogger<PostgresEmailVerificationStore> logger)
    : IEmailVerificationStore
{
    public async Task<VerificationResult> ReserveAsync(VerificationSubject subject, bool deliveryAvailable, string senderKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var available = options.Value.Enabled && deliveryAvailable;
            var now = PostgresTimestampHelper.UtcNow(clock);
            // Stable sender row is the first lock; resetting its UTC window happens after account admission.
            var sender = available ? await WindowAsync("sender:" + senderKey, now, TimeSpan.FromDays(1), true, cancellationToken) : null;
            var account = await VerificationPrincipalLock.AcquireAsync(db, subject, clock, cancellationToken);
            if (account is null) return new(VerificationOutcome.InvalidAuthentication);
            if (account.EmailConfirmed) return new(VerificationOutcome.AlreadyVerified);
            if (!available) return new(VerificationOutcome.Unavailable);
            now = PostgresTimestampHelper.UtcNow(clock);
            sender!.RenewIfExpired(now, TimeSpan.FromDays(1), true);
            var latest = await db.EmailVerificationChallenges.Where(value => value.UserId == subject.UserId)
                .OrderByDescending(value => value.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            var nextRequest = latest?.CreatedAt + EmailVerificationChallenge.Cooldown;
            if (latest?.State == EmailVerificationState.Pending && latest.PendingLeaseUntil > nextRequest)
                nextRequest = latest.PendingLeaseUntil;
            if (nextRequest > now) return new(VerificationOutcome.RateLimited, RetryAfter: nextRequest - now);

            var shortWindow = await WindowAsync("send15:" + subject.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture), now, TimeSpan.FromMinutes(15), false, cancellationToken);
            var dailyWindow = await WindowAsync("send24:" + subject.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture), now, TimeSpan.FromDays(1), false, cancellationToken);
            var limits = new[] { (sender, 100), (shortWindow, 5), (dailyWindow, 20) };
            var retry = MaximumRetry(limits, now);
            if (retry is not null) return new(VerificationOutcome.RateLimited, RetryAfter: retry);
            foreach (var (window, limit) in limits) window.Consume(now, limit);

            latest?.Invalidate(now);
            // Free the partial unique slot before inserting; both saves stay inside this transaction.
            await db.SaveChangesAsync(cancellationToken);
            var id = Guid.NewGuid();
            var material = protector.Create(subject.UserId, account.NormalizedEmail!, id);
            var challenge = new EmailVerificationChallenge(id, subject.UserId, account.Email!, account.NormalizedEmail!,
                material.Mac, material.KeyId, now);
            db.EmailVerificationChallenges.Add(challenge);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(VerificationOutcome.Reserved, id, challenge.ExpiresAt,
                Reservation: new PendingVerification(id, account.Email!, material.Code));
        }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
        {
            return DatabaseUnavailable();
        }
    }

    private async Task<EmailVerificationQuota> WindowAsync(string key, DateTimeOffset now, TimeSpan duration,
        bool utcDay, CancellationToken cancellationToken)
    {
        var start = utcDay ? new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero) : now;
        var end = start + duration;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO identity."email_verification_quotas" ("key", "window_start", "window_end", "count")
            VALUES ({key}, {start}, {end}, 0) ON CONFLICT ("key") DO NOTHING
            """, cancellationToken);
        var window = (await db.EmailVerificationQuotas.FromSqlInterpolated($"""
            SELECT * FROM identity."email_verification_quotas" WHERE "key" = {key} FOR UPDATE
            """).ToListAsync(cancellationToken)).Single();
        window.RenewIfExpired(now, duration, utcDay);
        return window;
    }

    private static TimeSpan? MaximumRetry(IEnumerable<(EmailVerificationQuota Window, int Limit)> windows, DateTimeOffset now)
    {
        var retry = TimeSpan.Zero;
        foreach (var (window, limit) in windows)
            if (window.RetryAfter(now, limit) is { } value && value > retry) retry = value;
        return retry > TimeSpan.Zero ? retry : null;
    }

    private VerificationResult DatabaseUnavailable()
    {
        db.ChangeTracker.Clear();
        logger.LogWarning("Identity email verification database is unavailable.");
        return new(VerificationOutcome.Unavailable);
    }

public async Task<VerificationResult> VerifyAsync(VerificationSubject subject, Guid challengeId, string code,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return new(VerificationOutcome.Unavailable);
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var account = await VerificationPrincipalLock.AcquireAsync(db, subject, clock, cancellationToken);
            if (account is null) return new(VerificationOutcome.InvalidAuthentication);
            var challenge = (await db.EmailVerificationChallenges.FromSqlInterpolated($"""
                SELECT * FROM identity."email_verification_challenges"
                WHERE "id" = {challengeId} AND "user_id" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var now = PostgresTimestampHelper.UtcNow(clock);
            var window = await WindowAsync("verify15:" + subject.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture), now, TimeSpan.FromMinutes(15), false, cancellationToken);
            if (window.RetryAfter(now, 20) is { } retry) return new(VerificationOutcome.RateLimited, RetryAfter: retry);
            window.Consume(now, 20);
            var validChallenge = !account.EmailConfirmed && challenge is not null && challenge.IsVerifiableAt(now)
                && challenge.Purpose == Domain.Entities.EmailVerificationChallenge.VerificationPurpose
                && challenge.EmailSnapshot == account.Email && challenge.NormalizedEmailSnapshot == account.NormalizedEmail;
            if (!validChallenge)
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(VerificationOutcome.InvalidCode);
            }
            if (!protector.Verify(challenge!, code))
            {
                challenge!.RejectCode(now);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(VerificationOutcome.InvalidCode);
            }
            challenge!.Consume(now);
            account.EmailConfirmed = true;
            account.UpdatedAt = now;
            account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(VerificationOutcome.Verified);
        }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
        {
            return DatabaseUnavailable();
        }
    }


public async Task<VerificationResult> CompleteAsync(VerificationSubject subject, Guid challengeId,
        VerificationDeliveryOutcome outcome, CancellationToken cancellationToken)
    {
        try
        {
            // Reservation objects were tracked before a network call. Reload state under fresh locks.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var account = await VerificationPrincipalLock.AcquireAsync(db, subject, clock, cancellationToken);
            var challenge = (await db.EmailVerificationChallenges.FromSqlInterpolated($"""
                SELECT * FROM identity."email_verification_challenges"
                WHERE "id" = {challengeId} AND "user_id" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var now = PostgresTimestampHelper.UtcNow(clock);
            if (challenge is null || challenge.State != EmailVerificationState.Pending)
                return new(account is null ? VerificationOutcome.InvalidAuthentication : VerificationOutcome.Unavailable);
            if (account is null || account.EmailConfirmed || now < challenge.CreatedAt || now >= challenge.ExpiresAt
                || account.Email != challenge.EmailSnapshot || account.NormalizedEmail != challenge.NormalizedEmailSnapshot)
            {
                challenge.EndSubmission(now < challenge.CreatedAt ? challenge.CreatedAt : now, failed: false);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(account is null ? VerificationOutcome.InvalidAuthentication : VerificationOutcome.Unavailable);
            }
            var accepted = outcome == VerificationDeliveryOutcome.Accepted;
            if (accepted) challenge.Submit(now);
            else challenge.EndSubmission(now, failed: true);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return accepted
                ? new(VerificationOutcome.Submitted, challenge.Id, challenge.ExpiresAt,
                    RetryAfter: TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling((challenge.CreatedAt + EmailVerificationChallenge.Cooldown - now).TotalSeconds))))
                : new(VerificationOutcome.Unavailable);
        }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
        {
            return DatabaseUnavailable();
        }
    }
}
