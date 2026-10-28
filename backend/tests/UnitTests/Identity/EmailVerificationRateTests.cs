using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.EmailVerification;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public sealed class EmailVerificationRateTests
{
    [Theory]
    [InlineData(false, 30)]
    [InlineData(true, 120)]
    public async Task IpAdmissionIsAtomicAndTheTwoRoutesHaveSeparateCounters(bool verify, int expected)
    {
        var clock = new Clock();
        var limiter = new EmailVerificationRateLimiter(Options.Create(new JwtOptions
            { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), clock);
        var results = await Task.WhenAll(Enumerable.Range(0, 200).Select(_ => Task.Run(() =>
            verify ? limiter.TryVerify("same-ip") : limiter.TrySend("same-ip"))));
        Assert.Equal(expected, results.Count(value => value is null));
        Assert.Null(verify ? limiter.TrySend("same-ip") : limiter.TryVerify("same-ip"));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Null(verify ? limiter.TryVerify("same-ip") : limiter.TrySend("same-ip"));
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
