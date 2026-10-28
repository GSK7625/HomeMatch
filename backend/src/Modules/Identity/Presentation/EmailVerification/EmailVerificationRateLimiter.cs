using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.EmailVerification;

public sealed class EmailVerificationRateLimiter(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly AtomicRequestQuota _send = new(options, clock, "identity-email-send", 30, TimeSpan.FromMinutes(1), 1, TimeSpan.FromMinutes(1));
    private readonly AtomicRequestQuota _verify = new(options, clock, "identity-email-verify", 120, TimeSpan.FromMinutes(1), 1, TimeSpan.FromMinutes(1));
    public TimeSpan? TrySend(string address) => _send.TryAcquire(address, null);
    public TimeSpan? TryVerify(string address) => _verify.TryAcquire(address, null);
}
