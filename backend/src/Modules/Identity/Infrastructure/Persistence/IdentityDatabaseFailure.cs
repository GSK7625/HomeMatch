using Npgsql;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

internal static class IdentityDatabaseFailure
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
