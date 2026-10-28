using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace HomeMatch.Modules.Identity.Infrastructure.Passwords;

public sealed class IdentityNewPasswordValidator(INewPasswordPolicy policy) : IPasswordValidator<IdentityAccount>
{
    public Task<IdentityResult> ValidateAsync(UserManager<IdentityAccount> manager, IdentityAccount user, string? password)
        => Task.FromResult(policy.IsAllowed(password) ? IdentityResult.Success : IdentityResult.Failed(
            new IdentityError { Code = "Identity.PasswordPolicy", Description = "Mật khẩu không đạt yêu cầu bảo mật." }));
}

/// <summary>Resolve and verify the embedded corpus during API startup, rather than silently allowing requests.</summary>
internal sealed class PasswordPolicyStartupCheck(INewPasswordPolicy policy) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = policy.IsAllowed("startup-policy-check");
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
