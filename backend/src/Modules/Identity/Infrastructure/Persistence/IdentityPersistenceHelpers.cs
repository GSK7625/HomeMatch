using HomeMatch.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

internal static class DatabaseFailureHelper
{
    public static bool IsUnavailable(Exception exception)
    {
        if (exception is TimeoutException) return true;
        if (exception is PostgresException postgres)
            return postgres.IsTransient || postgres.SqlState.StartsWith("08", StringComparison.Ordinal)
                || postgres.SqlState.StartsWith("28", StringComparison.Ordinal)
                || postgres.SqlState.StartsWith("53", StringComparison.Ordinal)
                || postgres.SqlState.StartsWith("57", StringComparison.Ordinal)
                || postgres.SqlState is "3D000" or "42501";
        if (exception is NpgsqlException) return true;
        return exception.InnerException is not null && IsUnavailable(exception.InnerException);
    }
}

internal static class PostgresTimestampHelper
{
    public static DateTimeOffset UtcNow(TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        // PostgreSQL stores microseconds; normalize before returning immutable session metadata.
        return new DateTimeOffset(now.UtcTicks - now.UtcTicks % 10, TimeSpan.Zero);
    }
}

/// <summary>Use only inside the caller's transaction/lock protocol; the caller saves tracked state and commits.</summary>
internal static class SessionRevocationHelper
{
    public static async Task RevokeFamilyAsync(IdentityDbContext database, AccountSession session,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        session.Revoke(now < session.CreatedAt ? session.CreatedAt : now);
        await database.RefreshCredentials.Where(value => value.SessionId == session.Id && value.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(value => value.RevokedAt,
                value => value.CreatedAt > now ? value.CreatedAt : now), cancellationToken);
    }

    public static async Task RevokeAccountAsync(IdentityDbContext database, long userId,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        await database.Sessions.Where(value => value.UserId == userId && value.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(value => value.RevokedAt,
                value => value.CreatedAt > now ? value.CreatedAt : now), cancellationToken);
        await database.RefreshCredentials.Where(value => value.RevokedAt == null
                && database.Sessions.Any(session => session.Id == value.SessionId && session.UserId == userId))
            .ExecuteUpdateAsync(update => update.SetProperty(value => value.RevokedAt,
                value => value.CreatedAt > now ? value.CreatedAt : now), cancellationToken);
    }
}
