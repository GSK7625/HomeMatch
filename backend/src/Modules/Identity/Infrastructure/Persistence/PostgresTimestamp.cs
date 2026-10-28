namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

internal static class PostgresTimestamp
{
    public static DateTimeOffset UtcNow(TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        // PostgreSQL stores microseconds; normalize before returning immutable session metadata.
        return new DateTimeOffset(now.UtcTicks - now.UtcTicks % 10, TimeSpan.Zero);
    }
}