using System.Text;
using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Application.Registration;

public sealed class NewPasswordPolicy : INewPasswordPolicy
{
    public bool IsAllowed(string? password)
    {
        if (password is null || password.Length > 256
            || !AccountInput.TryCountScalars(password, out var length) || length is < 8 or > 128)
            return false;

        var hasLetter = false;
        var hasDigit = false;
        foreach (var rune in password.EnumerateRunes())
        {
            hasLetter |= Rune.IsLetter(rune);
            hasDigit |= Rune.IsDigit(rune);
        }
        return hasLetter && hasDigit;
    }
}
