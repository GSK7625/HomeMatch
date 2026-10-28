using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

public sealed class EmailVerificationOptions
{
    public const string SectionName = "Identity:EmailVerification";
    public bool Enabled { get; set; }
    public string KeyId { get; set; } = "v1";
    public Dictionary<string, string> Keys { get; set; } = [];
}

public sealed class EmailVerificationOptionsValidator(IConfiguration configuration) : IValidateOptions<EmailVerificationOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailVerificationOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;
        if (!IsKeyId(options.KeyId) || !options.Keys.ContainsKey(options.KeyId) || options.Keys.Count > 4)
            return ValidateOptionsResult.Fail("An active OTP key and at most four OTP keys are required.");
        foreach (var (id, value) in options.Keys)
        {
            if (!IsKeyId(id) || value is null || value.Length > 1024) return InvalidKey();
            byte[] bytes;
            try { bytes = Convert.FromBase64String(value); }
            catch (FormatException) { return InvalidKey(); }
            try
            {
                if (bytes.Length < 32 || bytes.All(item => item == bytes[0])) return InvalidKey();
                var jwt = configuration["Jwt:Secret"];
                if (!string.IsNullOrEmpty(jwt))
                {
                    byte[] jwtBytes;
                    try { jwtBytes = Convert.FromBase64String(jwt); }
                    catch (FormatException) { return InvalidKey(); }
                    try { if (bytes.AsSpan().SequenceEqual(jwtBytes)) return InvalidKey(); }
                    finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(jwtBytes); }
                }
            }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
        }
        return ValidateOptionsResult.Success;
    }
    private static bool IsKeyId(string? value) => value is { Length: >= 1 and <= 32 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    private static ValidateOptionsResult InvalidKey() => ValidateOptionsResult.Fail("OTP keys must be independent random base64 keys of at least 32 bytes.");
}
