using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

public sealed record GeneratedRefreshSecret(string Raw, string Hash)
{
    public override string ToString() => "GeneratedRefreshSecret { [redacted] }";
}

public static class RefreshSecrets
{
    public static GeneratedRefreshSecret Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return new GeneratedRefreshSecret(Base64UrlEncoder.Encode(bytes), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    public static string? TryHash(string? raw)
    {
        if (raw is null || raw.Length != 43 || raw.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
            return null;
        try
        {
            var bytes = Base64UrlEncoder.DecodeBytes(raw);
            return bytes.Length == 32 && Base64UrlEncoder.Encode(bytes) == raw
                ? Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() : null;
        }
        catch (FormatException) { return null; }
    }
}
