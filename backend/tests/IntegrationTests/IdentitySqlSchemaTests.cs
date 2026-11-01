using HomeMatch.IntegrationTests.Infrastructure;
using Npgsql;

namespace HomeMatch.IntegrationTests;

public sealed class IdentitySqlSchemaTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task CoreTablesMatchTheBigIntSqlDesignAndSecurityExtensionsAreExplicit()
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT table_name,column_name,data_type,character_maximum_length,is_identity
            FROM information_schema.columns WHERE table_schema='identity'
            AND table_name IN ('users','user_profiles','user_tokens')
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new Dictionary<string, (string Type, int? Length, string Identity)>();
        while (await reader.ReadAsync()) columns[reader.GetString(0) + "." + reader.GetString(1)] =
            (reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetString(4));
        Assert.Equal("bigint", columns["users.id"].Type); Assert.Equal("YES", columns["users.id"].Identity);
        Assert.Equal("bigint", columns["user_profiles.user_id"].Type);
        Assert.Equal("bigint", columns["user_tokens.id"].Type); Assert.Equal("YES", columns["user_tokens.id"].Identity);
        Assert.Equal("bigint", columns["user_tokens.user_id"].Type);
        Assert.Equal(255, columns["users.email"].Length); Assert.Equal(255, columns["users.password_hash"].Length);
        Assert.Equal(20, columns["users.phone_number"].Length); Assert.Equal(20, columns["users.role"].Length);
        Assert.Equal(20, columns["users.status"].Length);
        Assert.Equal(64, columns["user_tokens.token_hash"].Length); Assert.Equal(30, columns["user_tokens.token_type"].Length);
        Assert.Equal(255, columns["user_tokens.device_info"].Length); Assert.Equal(45, columns["user_tokens.ip_address"].Length);
        Assert.Equal(8, columns.Keys.Count(key => key.StartsWith("user_tokens.")));
        Assert.Equal(9, columns.Keys.Count(key => key.StartsWith("user_profiles.")));
        Assert.Contains("users.email_confirmed", columns.Keys);
        Assert.Contains("users.security_version", columns.Keys);
        Assert.DoesNotContain("user_tokens.login_provider", columns.Keys);
        Assert.DoesNotContain("user_tokens.name", columns.Keys);
        Assert.DoesNotContain("user_profiles.phone_number", columns.Keys);
    }
}
