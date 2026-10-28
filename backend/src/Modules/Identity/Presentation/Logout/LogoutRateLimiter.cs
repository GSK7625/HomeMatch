using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.Logout;

public sealed class LogoutRateLimiter(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly AtomicRequestQuota _current = new(options, clock, "HomeMatch.Identity.LogoutQuota.v1",
        30, TimeSpan.FromMinutes(1), 1, TimeSpan.FromMinutes(1));
    private readonly AtomicRequestQuota _all = new(options, clock, "HomeMatch.Identity.LogoutAllQuota.v1",
        120, TimeSpan.FromMinutes(1), 5, TimeSpan.FromMinutes(15));

    public TimeSpan? TryCurrent(string clientAddress) => _current.TryAcquire(clientAddress, null);
    public TimeSpan? TryAll(string clientAddress, Guid userId) => _all.TryAcquire(clientAddress, userId.ToString("D"));
}
