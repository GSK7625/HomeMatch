using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.Login;

public sealed class LoginRateLimiter(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly AtomicRequestQuota _quota = new(options, clock, "HomeMatch.Identity.LoginQuota.v1",
        10, TimeSpan.FromMinutes(1), 5, TimeSpan.FromMinutes(1));
    public TimeSpan? TryAcquire(string clientAddress, string? normalizedEmail) => _quota.TryAcquire(clientAddress, normalizedEmail);
}
