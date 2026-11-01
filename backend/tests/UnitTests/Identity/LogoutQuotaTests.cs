using HomeMatch.Modules.Identity.Application.Authentication;
using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public class LogoutQuotaTests
{
    private static long _nextUserId;
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static IdentityQuotas Limiter(Clock clock) => new(Options.Create(new JwtOptions
        { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), clock);

    [Fact]
    public async Task CurrentLogoutQuotaIsAtomicAndIndependentFromLogoutAll()
    {
        var clock = new Clock();
        var limiter = Limiter(clock);
        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => limiter.TryLogoutCurrent("same-ip"))));
        Assert.Equal(30, results.Count(value => value is null));
        Assert.Equal(TimeSpan.FromMinutes(1), limiter.TryLogoutCurrent("same-ip"));
        Assert.Null(limiter.TryLogoutAll("same-ip", Interlocked.Increment(ref _nextUserId)));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Null(limiter.TryLogoutCurrent("same-ip"));
    }

    [Fact]
    public async Task LogoutAllUserQuotaIsSharedAcrossIpsAndResetsAfterFifteenMinutes()
    {
        var clock = new Clock();
        var limiter = Limiter(clock);
        var user = Interlocked.Increment(ref _nextUserId);
        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(index => Task.Run(() => limiter.TryLogoutAll("ip-" + index, user))));
        Assert.Equal(5, results.Count(value => value is null));
        Assert.Equal(TimeSpan.FromMinutes(15), limiter.TryLogoutAll("other", user));
        Assert.Null(limiter.TryLogoutAll("other", Interlocked.Increment(ref _nextUserId)));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Equal(TimeSpan.FromMinutes(14), limiter.TryLogoutAll("other", user));
        clock.Now = clock.Now.AddMinutes(14);
        Assert.Null(limiter.TryLogoutAll("other", user));
    }

    [Fact]
    public void LogoutAllAlsoHasAnIpCeilingAcrossDifferentUsers()
    {
        var limiter = Limiter(new Clock());
        for (var index = 0; index < 120; index++) Assert.Null(limiter.TryLogoutAll("shared-ip", Interlocked.Increment(ref _nextUserId)));
        Assert.NotNull(limiter.TryLogoutAll("shared-ip", Interlocked.Increment(ref _nextUserId)));
        Assert.Null(limiter.TryLogoutAll("different-ip", Interlocked.Increment(ref _nextUserId)));
    }
}
