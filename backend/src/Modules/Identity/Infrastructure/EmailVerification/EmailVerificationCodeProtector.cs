using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HomeMatch.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

public sealed class EmailVerificationCodeProtector(IOptions<EmailVerificationOptions> configured)
{
    public (string Code, string Mac, string KeyId) Create(Guid userId, string normalizedEmail, Guid challengeId)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var options = configured.Value;
        return (code, Compute(options.Keys[options.KeyId], userId, normalizedEmail, challengeId, code), options.KeyId);
    }

    public bool Verify(EmailVerificationChallenge challenge, string code)
    {
        if (code.Length != 6 || !code.All(char.IsAsciiDigit)
            || !configured.Value.Keys.TryGetValue(challenge.KeyId, out var key)) return false;
        var expected = Compute(key, challenge.UserId, challenge.NormalizedEmailSnapshot, challenge.Id, code);
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(challenge.CodeMac), Convert.FromHexString(expected));
    }

    private static string Compute(string encodedKey, Guid userId, string email, Guid challengeId, string code)
    {
        var key = Convert.FromBase64String(encodedKey);
        try
        {
            using var buffer = new MemoryStream();
            using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
            {
                // BinaryWriter prefixes each string length, so field boundaries cannot be confused.
                writer.Write(EmailVerificationChallenge.VerificationPurpose);
                writer.Write(userId.ToString("D")); writer.Write(email); writer.Write(challengeId.ToString("D")); writer.Write(code);
            }
            return Convert.ToHexString(HMACSHA256.HashData(key, buffer.ToArray()));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
