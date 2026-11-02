using System.Buffers;
using System.Net.Mail;
using System.Text;

namespace HomeMatch.Modules.Identity.Domain.Validation;

public static class AccountInput
{
    public static bool TryCountScalars(string? value, out int count)
    {
        count = 0;
        if (value is null) return false;
        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out _, out var consumed) != OperationStatus.Done) return false;
            count++;
            remaining = remaining[consumed..];
        }
        return true;
    }

    public static bool TryNormalizeEmail(string? input, out string email, out string normalizedEmail)
    {
        email = input?.Trim() ?? string.Empty;
        normalizedEmail = string.Empty;
        if (email.Length is 0 or > 254 || email.Any(character => character is < '!' or > '~')
            || email.Count(character => character == '@') != 1
            || !MailAddress.TryCreate(email, out var address)
            || address.DisplayName.Length != 0 || address.Address != email
            || address.User.StartsWith('.') || address.User.EndsWith('.') || address.User.Contains("..")
            || address.Host.StartsWith('.') || address.Host.EndsWith('.') || address.Host.Contains(".."))
            return false;

        normalizedEmail = email.ToUpperInvariant();
        return true;
    }

    public static bool TryNormalizeName(string? input, out string name)
    {
        name = input?.Trim() ?? string.Empty;
        return input is not null && !input.Any(char.IsControl)
            && TryCountScalars(name, out var count) && count is >= 2 and <= 100;
    }
}
