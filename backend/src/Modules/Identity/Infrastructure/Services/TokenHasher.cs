using System.Security.Cryptography;
using System.Text;

namespace HomeMatch.Modules.Identity.Infrastructure.Services;

public static class TokenHasher
{
    public static string GenerateSecureToken(int byteLength = 64)
    {
        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public static string HashToken(string token)
    {
        var inputBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
