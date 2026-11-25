using HomeMatch.Shared.Contracts.Properties;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class PostgresRoomOperationLock(IConfiguration configuration) : IRoomOperationLock
{
    public async Task<IAsyncDisposable> AcquireAsync(long roomId, CancellationToken cancellationToken = default)
    {
        // A dedicated connection holds the mutex while each module commits through its own DbContext.
        var settings = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
        {
            // Waiters must not exhaust the pool needed by the handler holding the lock.
            Pooling = false
        };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))", connection, transaction);
            command.Parameters.AddWithValue("key", $"properties:room-operation:{roomId}");
            await command.ExecuteNonQueryAsync(cancellationToken);
            return new Lease(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class Lease(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await transaction.DisposeAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
