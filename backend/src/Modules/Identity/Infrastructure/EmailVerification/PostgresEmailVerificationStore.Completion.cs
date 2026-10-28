using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

public sealed partial class PostgresEmailVerificationStore
{
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
                SELECT * FROM identity."EmailVerificationChallenges"
                WHERE "Id" = {challengeId} AND "UserId" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var now = PostgresTimestamp.UtcNow(clock);
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
        catch (Exception exception) when (IdentityDatabaseFailure.IsUnavailable(exception))
        {
            return DatabaseUnavailable();
        }
    }
}
