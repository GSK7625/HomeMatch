using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using HomeMatch.Modules.Identity.Application.Registration;

namespace HomeMatch.Modules.Identity.Infrastructure.Passwords;

public sealed class OfflinePasswordBlocklist : IPasswordBlocklist
{
    public const int ExpectedCount = 10908;
    public const string ExpectedSha256 = "8b53ba0f86311b36c33cf92dc6b123367f6590bc80fe3d0b37a79577630c964d";
    private readonly FrozenSet<string> _passwords;

    public OfflinePasswordBlocklist()
    {
        using var stream = typeof(OfflinePasswordBlocklist).Assembly.GetManifestResourceStream(
            "HomeMatch.Identity.CommonPasswords") ?? throw new InvalidOperationException("Password blocklist resource is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Password blocklist integrity check failed.");

        var lines = new UTF8Encoding(false, true).GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != ExpectedCount) throw new InvalidOperationException("Unexpected password blocklist count.");
        _passwords = lines.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    // Case-insensitive comparison catches common variants; the submitted password is never rewritten.
    public bool Contains(string password) => _passwords.Contains(password);
}
