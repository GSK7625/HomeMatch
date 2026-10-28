using System.Net;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMatch.IntegrationTests;

public partial class IdentityLoginTests
{
    [Fact]
    public async Task ConcurrentFailuresReachLockoutWithoutLostIncrementsAndDoNotRevokeExistingSession()
    {
        var account = await AccountAsync();
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var initial = await Login(client, account.Email!);
        var oldAccess = await AccessToken(initial);
        var failures = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var scope = database.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ILoginStore>().AuthenticateAndStartSessionAsync(
                new ValidatedLogin(account.Email!.ToUpperInvariant(), "incorrect", false), null, default);
        }));
        Assert.All(failures, value => Assert.Equal(LoginOutcome.InvalidCredentials, value.Outcome));
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var stored = await db.Users.SingleAsync(value => value.Id == account.Id);
            Assert.Equal(0, stored.AccessFailedCount);
            Assert.InRange(stored.LockoutEnd!.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(16));
        }
        using var blocked = await Login(client, account.Email!);
        await AssertError(blocked, HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldAccess);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task CorrectPasswordAfterLockoutExpiryClearsFailureState()
    {
        var account = await AccountAsync();
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var stored = await db.Users.SingleAsync(value => value.Id == account.Id);
            stored.LockoutEnd = DateTimeOffset.UtcNow.AddSeconds(-1);
            stored.AccessFailedCount = 2;
            await db.SaveChangesAsync();
        }
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var login = await Login(client, account.Email!);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        await using var verification = database.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var actual = await context.Users.SingleAsync(value => value.Id == account.Id);
        Assert.Equal(0, actual.AccessFailedCount);
        Assert.Null(actual.LockoutEnd);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulLoginReplacesOldCookieSessionIncludingWhenChangingAccounts(bool changeAccount)
    {
        var first = await AccountAsync();
        var next = changeAccount ? await AccountAsync() : first;
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var initial = await Login(client, first.Email!, remember: true);
        var oldAccess = await AccessToken(initial);
        var oldRaw = RefreshSecret(initial);
        SetRefresh(client, oldRaw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldAccess);
        using var replacement = await Login(client, next.Email!);
        var newAccess = await AccessToken(replacement);
        Assert.NotEqual(oldRaw, RefreshSecret(replacement));
        Assert.DoesNotContain("expires=", Assert.Single(replacement.Headers.GetValues("Set-Cookie")).ToLowerInvariant());
        using var oldMe = await client.GetAsync("/api/v1/identity/me");
        await AssertError(oldMe, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAccess);
        using var newMe = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, newMe.StatusCode);
        using var profile = JsonDocument.Parse(await newMe.Content.ReadAsStringAsync());
        Assert.Equal(next.Id, profile.RootElement.GetProperty("data").GetProperty("id").GetGuid());
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var oldHash = RefreshSecrets.TryHash(oldRaw)!;
        var oldId = await db.RefreshCredentials.Where(value => value.TokenHash == oldHash).Select(value => value.SessionId).SingleAsync();
        Assert.NotNull((await db.Sessions.SingleAsync(value => value.Id == oldId)).RevokedAt);
    }

    [Fact]
    public async Task FailedLoginKeepsOldCookieAndSessionUsable()
    {
        var first = await AccountAsync();
        var other = await AccountAsync();
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var initial = await Login(client, first.Email!);
        var access = await AccessToken(initial);
        SetRefresh(client, RefreshSecret(initial));
        using var failure = await Login(client, other.Email!, "incorrect");
        await AssertError(failure, HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task UnknownRefreshCookieDoesNotRevokeOtherDeviceSessions()
    {
        var account = await AccountAsync();
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var initial = await Login(client, account.Email!);
        var access = await AccessToken(initial);
        SetRefresh(client, RefreshSecrets.Create().Raw);
        using var next = await Login(client, account.Email!);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task IssuanceFailureRollsBackNewSessionAndOldSessionRevocation()
    {
        var first = await AccountAsync();
        var other = await AccountAsync();
        await using var original = Factory();
        using var firstClient = await BrowserAsync(original);
        using var initial = await Login(firstClient, first.Email!);
        var oldRaw = RefreshSecret(initial);
        var oldAccess = await AccessToken(initial);
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await db.Users.SingleAsync(value => value.Id == other.Id)).AccessFailedCount = 2;
            await db.SaveChangesAsync();
        }
        await using var failing = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccessTokenIssuer>();
            services.AddSingleton<IAccessTokenIssuer, ThrowingIssuer>();
        }));
        using var client = await BrowserAsync(failing);
        SetRefresh(client, oldRaw);
        using var failed = await Login(client, other.Email!);
        await AssertError(failed, HttpStatusCode.InternalServerError, "Server.InternalError");
        Assert.DoesNotContain("private-login-issuance-marker", await failed.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldAccess);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        await using var verify = database.Services.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.False(await context.Sessions.AnyAsync(value => value.UserId == other.Id));
        Assert.Equal(2, (await context.Users.SingleAsync(value => value.Id == other.Id)).AccessFailedCount);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong")]
    [InlineData("locked")]
    public async Task MissingWrongAndLockedCredentialsEachPerformHashVerification(string kind)
    {
        var account = await AccountAsync();
        if (kind == "locked")
        {
            await using var scope = database.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await db.Users.SingleAsync(value => value.Id == account.Id)).LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            await db.SaveChangesAsync();
        }
        var spy = new VerificationCounter();
        await using var original = Factory();
        await using var factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPasswordHasher<IdentityAccount>>();
            services.AddSingleton<IPasswordHasher<IdentityAccount>>(spy);
        }));
        using var client = await BrowserAsync(factory);
        using var failure = await Login(client, kind == "missing" ? Guid.NewGuid().ToString("N") + "@example.test" : account.Email!,
            kind == "wrong" ? "incorrect" : Password);
        await AssertError(failure, HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        Assert.Equal(1, spy.Verifications);
    }

    private sealed class ThrowingIssuer : IAccessTokenIssuer
    {
        public IssuedAccessToken Issue(AccessTokenSubject subject) => throw new InvalidOperationException("private-login-issuance-marker");
    }

    private sealed class VerificationCounter : IPasswordHasher<IdentityAccount>
    {
        private readonly PasswordHasher<IdentityAccount> _inner = new();
        private int _verifications;
        public int Verifications => _verifications;
        public string HashPassword(IdentityAccount user, string password) => _inner.HashPassword(user, password);
        public PasswordVerificationResult VerifyHashedPassword(IdentityAccount user, string hash, string password)
        { Interlocked.Increment(ref _verifications); return _inner.VerifyHashedPassword(user, hash, password); }
    }
}
