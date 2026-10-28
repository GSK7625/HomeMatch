using HomeMatch.Modules.Identity.Application.Refresh;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.Refresh;

public sealed class RefreshRateLimiter(IOptions<JwtOptions> options, TimeProvider clock) : IRefreshRequestQuota
{
    private readonly AtomicRequestQuota _quota = new(options, clock, "HomeMatch.Identity.RefreshQuota.v1",
        120, TimeSpan.FromMinutes(1), 30, TimeSpan.FromMinutes(1));

    public TimeSpan? TryAcquire(string clientAddress, Guid? sessionId)
        => _quota.TryAcquire(clientAddress, sessionId?.ToString("D"));
}
