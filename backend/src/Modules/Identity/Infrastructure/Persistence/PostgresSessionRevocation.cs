using HomeMatch.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

/// <summary>Use only inside the caller's transaction/lock protocol; the caller saves tracked state and commits.</summary>
internal static class PostgresSessionRevocation
{
    public static async Task RevokeFamilyAsync(IdentityDbContext database, AccountSession session,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        session.Revoke(now < session.CreatedAt ? session.CreatedAt : now);
        await database.RefreshCredentials.Where(value => value.SessionId == session.Id && value.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(value => value.RevokedAt,
                value => value.CreatedAt > now ? value.CreatedAt : now), cancellationToken);
    }

    public static async Task RevokeAccountAsync(IdentityDbContext database, Guid userId,
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
