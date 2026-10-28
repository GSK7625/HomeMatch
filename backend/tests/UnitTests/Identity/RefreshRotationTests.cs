using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Refresh;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public class RefreshRotationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
    private static RefreshCredential Credential(Guid session, DateTimeOffset? created = null, DateTimeOffset? expires = null)
        => new(session, RefreshSecrets.Create().Hash, created ?? Now, expires ?? Now.AddDays(1));

    [Fact]
    public void ConsumptionIsOneWayAndLinksOnlyOneSuccessor()
    {
        var old = Credential(Guid.NewGuid());
        Assert.False(old.IsUsableAt(Now.AddTicks(-1)));
        Assert.True(old.IsUsableAt(Now));
        old.Consume(Now.AddMinutes(1));
        Assert.False(old.IsUsableAt(Now.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() => old.Consume(Now.AddMinutes(2)));
        var next = Credential(old.SessionId, Now.AddMinutes(1));
        old.LinkReplacement(next);
        Assert.Equal(next.Id, old.ReplacedByCredentialId);
        Assert.Throws<InvalidOperationException>(() => old.LinkReplacement(Credential(old.SessionId, Now.AddMinutes(2))));
    }

    [Theory]
    [InlineData("future")]
    [InlineData("expired")]
    public void UnusableCredentialCannotBeConsumed(string reason)
    {
        var credential = Credential(Guid.NewGuid(), reason == "future" ? Now.AddMinutes(1) : Now.AddDays(-2),
            reason == "expired" ? Now : Now.AddDays(1));
        Assert.Throws<InvalidOperationException>(() => credential.Consume(Now));
        Assert.Null(credential.UsedAt);
    }

    [Theory]
    [InlineData("unconsumed")]
    [InlineData("family")]
    [InlineData("expiry")]
    [InlineData("time")]
    [InlineData("used-successor")]
    public void ReplacementMustBelongToSameAbsoluteLivedFamily(string reason)
    {
        var old = Credential(Guid.NewGuid());
        if (reason != "unconsumed") old.Consume(Now.AddMinutes(1));
        var next = Credential(reason == "family" ? Guid.NewGuid() : old.SessionId,
            reason == "time" ? Now : Now.AddMinutes(1), reason == "expiry" ? Now.AddDays(2) : Now.AddDays(1));
        if (reason == "used-successor") next.Consume(Now.AddMinutes(1));
        Assert.Throws<InvalidOperationException>(() => old.LinkReplacement(next));
    }

    [Fact]
    public void FractionalSecondAtSessionEndCannotBecomeAZeroLifetimeJwt()
    {
        var clock = new Clock { Now = Now };
        var issuer = new JwtAccessTokenIssuer(Options.Create(new JwtOptions
            { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), clock);
        Assert.Throws<AccessTokenSessionExpiredException>(() => issuer.Issue(new AccessTokenSubject(
            Guid.NewGuid(), Guid.NewGuid(), "Tenant", 1, Now.AddMilliseconds(900))));
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task RefreshSessionQuotaIsAtomicAcrossIpsAndExpiresWithoutChangingOtherSessions()
    {
        var clock = new Clock();
        var limiter = new RefreshRateLimiter(Options.Create(new JwtOptions
            { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), clock);
        var session = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(index => Task.Run(() => limiter.TryAcquire("ip-" + index, session))));
        Assert.Equal(30, results.Count(result => result is null));
        Assert.Equal(TimeSpan.FromMinutes(1), limiter.TryAcquire("other", session));
        Assert.Null(limiter.TryAcquire("other", Guid.NewGuid()));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Null(limiter.TryAcquire("other", session));
    }

    [Fact]
    public void RefreshIpQuotaAlsoCoversUnknownTokens()
    {
        var limiter = new RefreshRateLimiter(Options.Create(new JwtOptions
            { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), new Clock());
        for (var index = 0; index < 120; index++) Assert.Null(limiter.TryAcquire("same", null));
        Assert.NotNull(limiter.TryAcquire("same", Guid.NewGuid()));
        Assert.Null(limiter.TryAcquire("different", null));
    }
}
