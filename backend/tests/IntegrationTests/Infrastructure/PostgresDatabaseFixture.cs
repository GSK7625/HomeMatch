using System.Security.Cryptography;
using HomeMatch.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HomeMatch.IntegrationTests.Infrastructure;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = "homematch_identity_test_" + Guid.NewGuid().ToString("N");
    private TemporaryPostgresCluster? _cluster;
    private string _adminConnection = string.Empty;
    private bool _databaseCreated;
    private ServiceProvider? _services;

    public ServiceProvider Services => _services ?? throw new InvalidOperationException("Test database has not been initialized.");
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            var supplied = Environment.GetEnvironmentVariable("HOMEMATCH_TEST_POSTGRES_CONNECTION");
            if (string.IsNullOrWhiteSpace(supplied))
            {
                _cluster = new TemporaryPostgresCluster();
                await _cluster.StartAsync();
                supplied = _cluster.ConnectionString;
            }

            var admin = new NpgsqlConnectionStringBuilder(supplied)
            {
                Pooling = false, IncludeErrorDetail = false,
                ApplicationName = "HomeMatch.Identity.Tests"
            };
            if (string.IsNullOrWhiteSpace(admin.Database)) admin.Database = "postgres";
            _adminConnection = admin.ConnectionString;
            await using (var connection = new NpgsqlConnection(_adminConnection))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
                await command.ExecuteNonQueryAsync();
                _databaseCreated = true;
            }

            admin.Database = _databaseName;
            ConnectionString = admin.ConnectionString;
            await using (var connection = new NpgsqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                await using var sentinel = connection.CreateCommand();
                sentinel.CommandText = """
                    CREATE SCHEMA t04_neighbor;
                    CREATE TABLE t04_neighbor."Sentinel" ("Value" integer NOT NULL);
                    INSERT INTO t04_neighbor."Sentinel" VALUES (42);
                    """;
                await sentinel.ExecuteNonQueryAsync();
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ["Jwt:Issuer"] = "HomeMatch.Tests",
                ["Jwt:Audience"] = "HomeMatch.Tests.Client"
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIdentityModule(configuration);
            _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            await Services.InitializeIdentityDatabaseAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
            _services = null;
        }

        try
        {
            if (_databaseCreated)
            {
                // Never drop a caller-supplied database; only this fixture's newly created UUID name.
                if (!_databaseName.StartsWith("homematch_identity_test_", StringComparison.Ordinal)
                    || !Guid.TryParseExact(_databaseName["homematch_identity_test_".Length..], "N", out _))
                {
                    throw new InvalidOperationException("Unexpected test database name.");
                }

                await using var connection = new NpgsqlConnection(_adminConnection);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"DROP DATABASE \"{_databaseName}\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync();
                _databaseCreated = false;
            }
        }
        finally
        {
            if (_cluster is not null)
            {
                await _cluster.DisposeAsync();
                _cluster = null;
            }
        }
    }
}