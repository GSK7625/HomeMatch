using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

public sealed partial class PostgresEmailVerificationStore
{
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
                SELECT * FROM identity."EmailVerificationChallenges"
                WHERE "Id" = {challengeId} AND "UserId" = {subject.UserId} FOR UPDATE
                """).ToListAsync(cancellationToken)).SingleOrDefault();
            var now = PostgresTimestamp.UtcNow(clock);
            var window = await WindowAsync("verify15:" + subject.UserId.ToString("N"), now, TimeSpan.FromMinutes(15), false, cancellationToken);
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
        catch (Exception exception) when (IdentityDatabaseFailure.IsUnavailable(exception))
        {
            return DatabaseUnavailable();
        }
    }
}
