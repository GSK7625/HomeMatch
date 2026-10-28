using System.Security.Cryptography;
using System.Text;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Login;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public class LoginPolicyTests
{
    [Fact]
    public void RefreshSecretsAreCanonicalRandomTokensAndHashRawBytes()
    {
        var first = RefreshSecrets.Create();
        var second = RefreshSecrets.Create();
        Assert.Matches("^[A-Za-z0-9_-]{43}$", first.Raw);
        Assert.Matches("^[0-9a-f]{64}$", first.Hash);
        Assert.NotEqual(first.Raw, second.Raw);
        Assert.Equal(first.Hash, RefreshSecrets.TryHash(first.Raw));
        Assert.NotEqual(first.Raw, first.Hash);
        Assert.DoesNotContain(first.Raw, first.ToString());
        Assert.Null(RefreshSecrets.TryHash(new string('A', 42) + "B")); // non-canonical unused bits
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    public void MalformedRefreshSecretsCannotSelectASession(string? value)
        => Assert.Null(RefreshSecrets.TryHash(value));

    [Theory]
    [InlineData("id")]
    [InlineData("hash")]
    [InlineData("expiry")]
    public void RefreshCredentialRejectsInvalidState(string field)
    {
        var now = DateTimeOffset.UtcNow;
        Assert.ThrowsAny<ArgumentException>(() => new RefreshCredential(field == "id" ? Guid.Empty : Guid.NewGuid(),
            field == "hash" ? "not-a-hash" : new string('a', 64), now, field == "expiry" ? now : now.AddDays(1)));
    }

    private sealed class Store : ILoginStore
    {
        public ValidatedLogin? Last;
        public Task<LoginResult> AuthenticateAndStartSessionAsync(ValidatedLogin login, string? previous, CancellationToken cancellationToken)
        { Last = login; return Task.FromResult(new LoginResult(LoginOutcome.InvalidCredentials)); }
    }

    [Theory]
    [InlineData("old")]
    [InlineData("123456789012345")]
    [InlineData("  Mật khẩu cũ giữ nguyên  ")]
    public async Task LoginDoesNotApplyNewPasswordMinimumOrBlocklist(string password)
    {
        var store = new Store();
        await new LoginHandler(store).HandleAsync(new LoginCommand(" User+tag@Example.test ", password, false), null, default);
        Assert.Equal("USER+TAG@EXAMPLE.TEST", store.Last!.NormalizedEmail);
        Assert.Equal(password, store.Last.Password);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("password")]
    [InlineData("empty")]
    [InlineData("length")]
    [InlineData("remember")]
    public async Task InvalidLoginInputCannotReachStore(string invalid)
    {
        var store = new Store();
        var result = await new LoginHandler(store).HandleAsync(new LoginCommand(invalid == "email" ? "invalid" : "a@example.test",
            invalid == "password" ? null : invalid == "empty" ? "" : invalid == "length" ? new string('a', 129) : "old",
            invalid == "remember" ? null : false), null, default);
        Assert.Equal(LoginOutcome.InvalidInput, result.Outcome);
        Assert.Null(store.Last);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task LoginQuotasAreAtomicForConcurrentRequestsAndResetAfterOneMinute()
    {
        var clock = new Clock();
        var limiter = new LoginRateLimiter(Options.Create(new JwtOptions
            { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), clock);
        var results = await Task.WhenAll(Enumerable.Range(0, 30).Select(index =>
            Task.Run(() => limiter.TryAcquire("ip-" + index, "EMAIL@EXAMPLE.TEST"))));
        Assert.Equal(5, results.Count(value => value is null));
        Assert.Equal(TimeSpan.FromMinutes(1), limiter.TryAcquire("other-ip", "EMAIL@EXAMPLE.TEST"));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Null(limiter.TryAcquire("other-ip", "EMAIL@EXAMPLE.TEST"));
        for (var index = 0; index < 9; index++) Assert.Null(limiter.TryAcquire("other-ip", "different-" + index));
        Assert.Equal(TimeSpan.FromMinutes(1), limiter.TryAcquire("other-ip", "final"));
    }
}
