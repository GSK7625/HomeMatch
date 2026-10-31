using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HomeMatch.IntegrationTests;

public sealed class IdentityBigIntMigrationTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task UpgradePreservesUserProfileAndTokenHistoryAndRollbackProtectsNewWrites()
    {
        var name = "homematch_bigint_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(database.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = name, Pooling = false };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection.ConnectionString
            }).Build();
            var services = new ServiceCollection(); services.AddLogging(); services.AddIdentityModule(configuration);
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var migrator = db.GetService<IMigrator>();
            const string previous = "20261031114352_NormalizeIdentitySnakeCase";
            await migrator.MigrateAsync(previous);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE SCHEMA neighbor;
                CREATE TABLE neighbor.sentinel(value integer NOT NULL);
                INSERT INTO neighbor.sentinel VALUES (42);
                INSERT INTO identity.users(id,status,security_version,created_at,updated_at,email_confirmed,
                  phone_number_confirmed,two_factor_enabled,lockout_enabled,access_failed_count,user_name,normalized_user_name,
                  email,normalized_email,password_hash,security_stamp,concurrency_stamp)
                VALUES ('11111111-1111-1111-1111-111111111111',1,1,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,true,false,false,false,0,
                  'legacy@example.test','LEGACY@EXAMPLE.TEST','legacy@example.test','LEGACY@EXAMPLE.TEST','preserved-hash','stamp','stamp');
                INSERT INTO identity.roles(id,name,normalized_name,concurrency_stamp)
                VALUES ('22222222-2222-2222-2222-222222222222','Landlord','LANDLORD','stamp');
                INSERT INTO identity.user_roles(user_id,role_id)
                VALUES ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222');
                INSERT INTO identity.user_profiles(user_id,full_name,phone_number,created_at,updated_at)
                VALUES ('11111111-1111-1111-1111-111111111111','Legacy User','+84912345678',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP);
                INSERT INTO identity.sessions(id,user_id,security_version,created_at,expires_at)
                VALUES ('33333333-3333-3333-3333-333333333333','11111111-1111-1111-1111-111111111111',1,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP+interval '1 day');
                INSERT INTO identity.refresh_credentials(id,session_id,token_hash,created_at,expires_at)
                VALUES ('44444444-4444-4444-4444-444444444444','33333333-3333-3333-3333-333333333333',repeat('a',64),CURRENT_TIMESTAMP,CURRENT_TIMESTAMP+interval '1 day');
                """);
            await migrator.MigrateAsync();
            await using var check = new NpgsqlConnection(connection.ConnectionString); await check.OpenAsync();
            await using (var command = new NpgsqlCommand("""
                SELECT u.id,u.role,u.password_hash,u.phone_number,u.security_version,p.full_name,
                  (SELECT count(*) FROM identity.user_tokens WHERE token_type='REFRESH'),
                  (SELECT revoked_at IS NOT NULL FROM identity.sessions LIMIT 1),
                  (SELECT value FROM neighbor.sentinel)
                FROM identity.users u JOIN identity.user_profiles p ON p.user_id=u.id
                """, check))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync()); Assert.Equal(1L, reader.GetInt64(0));
                Assert.Equal("LANDLORD", reader.GetString(1)); Assert.Equal("preserved-hash", reader.GetString(2));
                Assert.Equal("+84912345678", reader.GetString(3)); Assert.Equal(2L, reader.GetInt64(4));
                Assert.Equal("Legacy User", reader.GetString(5)); Assert.Equal(1L, reader.GetInt64(6));
                Assert.True(reader.GetBoolean(7)); Assert.Equal(42, reader.GetInt32(8));
            }
            await migrator.MigrateAsync(previous);
            await using (var restored = new NpgsqlCommand("SELECT id FROM identity.users", check))
                Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), await restored.ExecuteScalarAsync());
            await migrator.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync("UPDATE identity.user_profiles SET full_name='Changed after upgrade'");
            var failure = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(previous));
            Assert.Equal("P0001", failure.SqlState);
            await using var retained = new NpgsqlCommand("SELECT full_name FROM identity.user_profiles", check);
            Assert.Equal("Changed after upgrade", await retained.ExecuteScalarAsync());
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
