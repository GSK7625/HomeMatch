using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Passwords;

public sealed class IdentityNewPasswordValidator(INewPasswordPolicy policy) : IPasswordValidator<IdentityAccount>
{
    public Task<IdentityResult> ValidateAsync(UserManager<IdentityAccount> manager, IdentityAccount user, string? password)
        => Task.FromResult(policy.IsAllowed(password) ? IdentityResult.Success : IdentityResult.Failed(
            new IdentityError { Code = "Identity.PasswordPolicy", Description = "Mật khẩu không đạt yêu cầu bảo mật." }));
}
