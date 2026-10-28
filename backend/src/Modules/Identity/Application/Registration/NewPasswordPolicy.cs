using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Application.Registration;

/// <summary>Only for establishing/changing credentials; never apply new policy to login verification.</summary>
public sealed class NewPasswordPolicy(IPasswordBlocklist blocklist) : INewPasswordPolicy
{
    public bool IsAllowed(string? password)
        => password is not null && password.Length <= 256
            && AccountInput.TryCountScalars(password, out var length) && length is >= 15 and <= 128
            && !blocklist.Contains(password);
}
