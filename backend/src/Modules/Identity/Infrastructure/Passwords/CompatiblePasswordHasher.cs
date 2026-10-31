using System.Text;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.Passwords;

/// <summary>Reads legacy Identity/SQL hashes and writes versioned BCrypt-SHA384 hashes.</summary>
public sealed class CompatiblePasswordHasher(IOptions<PasswordHasherOptions> options) : IPasswordHasher<IdentityAccount>
{
    private const string Prefix = "{bcrypt-sha384}";
    private const int WorkFactor = 12;
    private readonly PasswordHasher<IdentityAccount> _legacy = new(options);

    public string HashPassword(IdentityAccount user, string password)
        => Prefix + BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor, BCrypt.Net.HashType.SHA384);

    public PasswordVerificationResult VerifyHashedPassword(IdentityAccount user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword)) return PasswordVerificationResult.Failed;
        try
        {
            if (hashedPassword.StartsWith(Prefix, StringComparison.Ordinal))
            {
                var hash = hashedPassword[Prefix.Length..];
                if (!BCrypt.Net.BCrypt.EnhancedVerify(providedPassword, hash, BCrypt.Net.HashType.SHA384))
                    return PasswordVerificationResult.Failed;
                return BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, WorkFactor)
                    ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
            }

            if (hashedPassword.StartsWith("$2", StringComparison.Ordinal))
            {
                // Raw BCrypt cannot distinguish suffixes beyond its 72-byte input limit.
                // Existing long raw-BCrypt passwords must use the OTP password-reset flow.
                return Encoding.UTF8.GetByteCount(providedPassword) <= 72 && BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                    ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Failed;
            }

            return _legacy.VerifyHashedPassword(user, hashedPassword, providedPassword) == PasswordVerificationResult.Failed
                ? PasswordVerificationResult.Failed : PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (Exception error) when (error is FormatException or ArgumentException or BCrypt.Net.SaltParseException or BCrypt.Net.HashInformationException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
