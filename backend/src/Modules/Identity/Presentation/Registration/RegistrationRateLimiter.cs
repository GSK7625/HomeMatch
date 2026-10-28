using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.Registration;

/// <summary>One-instance, atomic acquisition of IP and email windows. Keys never contain plaintext email.</summary>
public sealed class RegistrationRateLimiter(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly AtomicRequestQuota _quota = new(options, clock, "HomeMatch.Identity.RegistrationQuota.v1",
        5, TimeSpan.FromMinutes(15), 3, TimeSpan.FromHours(1));
    public TimeSpan? TryAcquire(string clientAddress, string? normalizedEmail) => _quota.TryAcquire(clientAddress, normalizedEmail);
}
