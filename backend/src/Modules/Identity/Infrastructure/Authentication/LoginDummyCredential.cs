using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

/// <summary>One real Identity hash, warmed before serving requests; no known account/password is provisioned.</summary>
public sealed class LoginDummyCredential
{
    private readonly IdentityAccount _subject = new("dummy@example.invalid", DateTimeOffset.UnixEpoch);
    private readonly string _hash;

    public LoginDummyCredential(IOptions<PasswordHasherOptions> options)
    {
        _hash = new PasswordHasher<IdentityAccount>(options).HashPassword(_subject,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public void Verify(IPasswordHasher<IdentityAccount> hasher, string password)
        => _ = hasher.VerifyHashedPassword(_subject, _hash, password);
}

internal sealed class LoginSecurityStartupCheck(LoginDummyCredential credential) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = credential;
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
