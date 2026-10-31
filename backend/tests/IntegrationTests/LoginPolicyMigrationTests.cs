using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace HomeMatch.IntegrationTests;

public class LoginPolicyMigrationTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task UpgradeRevokesRememberedSessionsClearsLockoutAndKeepsNormalSessions()
    {
        var name = "homematch_policy_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(database.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = name, Pooling = false };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection.ConnectionString
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIdentityModule(configuration);
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("20261028113916_AddEmailVerification");
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO identity."Users" ("Id", "Status", "SecurityVersion", "CreatedAt", "UpdatedAt",
                    "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount", "LockoutEnd",
                    "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "PasswordHash", "SecurityStamp", "ConcurrencyStamp")
                VALUES ('11111111-1111-1111-1111-111111111111', 1, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,
                    FALSE, FALSE, FALSE, TRUE, 4, CURRENT_TIMESTAMP + INTERVAL '15 minutes',
                    'legacy@example.test', 'LEGACY@EXAMPLE.TEST', 'legacy@example.test', 'LEGACY@EXAMPLE.TEST',
                    'migration-preserved-hash', 'security-stamp', 'concurrency-stamp');
                INSERT INTO identity."Sessions" ("Id", "UserId", "SecurityVersion", "RememberMe", "CreatedAt", "ExpiresAt")
                VALUES ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 1, TRUE,
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '30 days'),
                    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 1, FALSE,
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '24 hours');
                INSERT INTO identity."RefreshCredentials" ("Id", "SessionId", "TokenHash", "CreatedAt", "ExpiresAt")
                VALUES ('44444444-4444-4444-4444-444444444444', '22222222-2222-2222-2222-222222222222', repeat('a', 64),
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '30 days');
                """);
            await migrator.MigrateAsync("20261029120005_SimplifyLoginPolicy");
            await using var check = new NpgsqlConnection(connection.ConnectionString);
            await check.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT "LockoutEnabled", "PasswordHash", "SecurityVersion", "AccessFailedCount", "LockoutEnd",
                (SELECT "RevokedAt" IS NOT NULL FROM identity."Sessions" WHERE "Id" = '22222222-2222-2222-2222-222222222222')
                FROM identity."Users"
                """, check);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.False(reader.GetBoolean(0));
            Assert.Equal("migration-preserved-hash", reader.GetString(1));
            Assert.Equal(1L, reader.GetInt64(2));
            Assert.Equal(0, reader.GetInt32(3));
            Assert.True(reader.IsDBNull(4));
            Assert.True(reader.GetBoolean(5));
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
