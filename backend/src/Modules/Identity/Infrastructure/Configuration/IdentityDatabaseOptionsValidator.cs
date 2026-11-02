using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeMatch.Modules.Identity.Infrastructure.Configuration;

public sealed class IdentityDatabaseOptionsValidator : IValidateOptions<IdentityDatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentityDatabaseOptions options)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(options.DefaultConnection) && options.DefaultConnection.Length <= 8192)
            {
                var connection = new NpgsqlConnectionStringBuilder(options.DefaultConnection);
                if (!string.IsNullOrWhiteSpace(connection.Host)
                    && !string.IsNullOrWhiteSpace(connection.Database)
                    && !string.IsNullOrWhiteSpace(connection.Username)
                    && connection.Port is >= 1 and <= 65535)
                {
                    return ValidateOptionsResult.Success;
                }
            }
        }
        catch (ArgumentException)
        {
            // Parser errors can contain input values. Never return the original exception.
        }

        return ValidateOptionsResult.Fail(
            "ConnectionStrings:DefaultConnection must be a valid PostgreSQL connection string with Host, Database and Username.");
    }
}