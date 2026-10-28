using System.Collections.Frozen;
using HomeMatch.Modules.Identity.Application.Registration;

namespace HomeMatch.Modules.Identity.Infrastructure.Passwords;

public sealed class OfflinePasswordBlocklist : IPasswordBlocklist
{
    private static readonly FrozenSet<string> Passwords = new[]
    {
        "123456789012345",
        "1234567890123456",
        "password1234567",
        "password12345678",
        "admin1234567890"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public bool Contains(string password) => Passwords.Contains(password);
}
