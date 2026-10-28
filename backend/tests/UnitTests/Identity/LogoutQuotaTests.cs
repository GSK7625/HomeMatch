using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Logout;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public class LogoutQuotaTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static LogoutRateLimiter Limiter(Clock clock) => new(Options.Create(new JwtOptions
        { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), clock);

    [Fact]
    public async Task CurrentLogoutQuotaIsAtomicAndIndependentFromLogoutAll()
    {
        var clock = new Clock();
        var limiter = Limiter(clock);
        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => limiter.TryCurrent("same-ip"))));
        Assert.Equal(30, results.Count(value => value is null));
        Assert.Equal(TimeSpan.FromMinutes(1), limiter.TryCurrent("same-ip"));
        Assert.Null(limiter.TryAll("same-ip", Guid.NewGuid()));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Null(limiter.TryCurrent("same-ip"));
    }

    [Fact]
    public async Task LogoutAllUserQuotaIsSharedAcrossIpsAndResetsAfterFifteenMinutes()
    {
        var clock = new Clock();
        var limiter = Limiter(clock);
        var user = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(index => Task.Run(() => limiter.TryAll("ip-" + index, user))));
        Assert.Equal(5, results.Count(value => value is null));
        Assert.Equal(TimeSpan.FromMinutes(15), limiter.TryAll("other", user));
        Assert.Null(limiter.TryAll("other", Guid.NewGuid()));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Equal(TimeSpan.FromMinutes(14), limiter.TryAll("other", user));
        clock.Now = clock.Now.AddMinutes(14);
        Assert.Null(limiter.TryAll("other", user));
    }

    [Fact]
    public void LogoutAllAlsoHasAnIpCeilingAcrossDifferentUsers()
    {
        var limiter = Limiter(new Clock());
        for (var index = 0; index < 120; index++) Assert.Null(limiter.TryAll("shared-ip", Guid.NewGuid()));
        Assert.NotNull(limiter.TryAll("shared-ip", Guid.NewGuid()));
        Assert.Null(limiter.TryAll("different-ip", Guid.NewGuid()));
    }
}
