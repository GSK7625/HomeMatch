using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.Configuration;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (!HasValidSigningKey(options.Secret))
        {
            failures.Add("Jwt:Secret must be a base64-encoded random key of at least 32 bytes; do not use an example or placeholder.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (options.ExpirationMinutes is < 1 or > 15)
        {
            failures.Add("Jwt:ExpirationMinutes must be between 1 and 15.");
        }

        if (options.ClockSkewSeconds is < 0 or > 30)
        {
            failures.Add("Jwt:ClockSkewSeconds must be between 0 and 30.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool HasValidSigningKey(string? secret)
    {
        // Bound input before allocating and reject placeholders without including values in errors.
        if (string.IsNullOrWhiteSpace(secret) || secret.Length > 1024 || secret.Any(char.IsWhiteSpace))
        {
            return false;
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(secret);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            // This rejects trivial keys, not a substitute for generating keys with a CSPRNG.
            return key.Length >= 32 && key.Any(value => value != key[0]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
