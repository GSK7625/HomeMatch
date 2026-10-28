using System.Net;
using System.Net.Http.Json;
using System.Text;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

public partial class IdentityRefreshTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("malformed")]
    [InlineData("unknown")]
    public async Task MissingMalformedOrUnknownCookieReturns401AndDeletesCookie(string? kind)
    {
        await using var factory = Factory();
        using var client = await Browser(factory);
        if (kind is not null) SetRefresh(client, kind == "unknown" ? RefreshSecrets.Create().Raw : "bad");
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(response, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
    }

    [Theory]
    [InlineData("origin")]
    [InlineData("csrf")]
    [InlineData("cookie")]
    public async Task BadOriginOrCsrfDoesNotConsumeOrRevokeRefreshCredential(string invalid)
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account);
        var raw = Raw(login);
        SetRefresh(client, raw);
        client.DefaultRequestHeaders.Remove(invalid == "origin" ? "Origin" : invalid == "csrf" ? "X-CSRF-TOKEN" : "Cookie");
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(response, HttpStatusCode.Forbidden, invalid == "origin" ? "Identity.OriginNotAllowed" : "Identity.CsrfInvalid");
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hash = RefreshSecrets.TryHash(raw)!;
        var credential = await db.RefreshCredentials.SingleAsync(value => value.TokenHash == hash);
        Assert.Null(credential.UsedAt);
        Assert.Null(credential.RevokedAt);
        Assert.Null((await db.Sessions.SingleAsync(value => value.Id == credential.SessionId)).RevokedAt);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("{\"refreshToken\":\"not-accepted\"}")]
    [InlineData("{\"rememberMe\":false}")]
    [InlineData("{\"userId\":\"not-accepted\"}")]
    public async Task RefreshOnlyAcceptsEmptyObjectOrOmittedBody(string json)
    {
        await using var factory = Factory(available: false);
        using var client = await Browser(factory);
        SetRefresh(client, RefreshSecrets.Create().Raw);
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertFailure(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
    }

    [Fact]
    public async Task RefreshMediaTypeAndSizeErrorsDoNotClearCookie()
    {
        await using var factory = Factory(available: false);
        using var client = await Browser(factory);
        using var media = await client.PostAsync("/api/v1/identity/auth/refresh", new StringContent("{}"));
        await AssertFailure(media, HttpStatusCode.UnsupportedMediaType, "Identity.UnsupportedMediaType");
        using var large = await client.PostAsync("/api/v1/identity/auth/refresh",
            new StringContent(new string(' ', 16385), Encoding.UTF8, "application/json"));
        await AssertFailure(large, HttpStatusCode.RequestEntityTooLarge, "Identity.RequestTooLarge");
    }

    [Fact]
    public async Task DatabaseUnavailableDoesNotRotateOrClearCookie()
    {
        await using var factory = Factory(available: false);
        using var client = await Browser(factory);
        SetRefresh(client, RefreshSecrets.Create().Raw);
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
    }

    [Theory]
    [InlineData("suspended")]
    [InlineData("version")]
    [InlineData("revoked-session")]
    [InlineData("revoked-credential")]
    [InlineData("session-expired")]
    [InlineData("credential-expired")]
    [InlineData("profile")]
    [InlineData("membership")]
    public async Task InvalidAccountOrSessionCannotRefresh(string reason)
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account);
        var raw = Raw(login);
        SetRefresh(client, raw);
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var hash = RefreshSecrets.TryHash(raw)!;
            var credential = await db.RefreshCredentials.SingleAsync(value => value.TokenHash == hash);
            if (reason == "suspended")
                await db.Users.Where(value => value.Id == account.Id).ExecuteUpdateAsync(update => update.SetProperty(value => value.Status, AccountStatus.Suspended));
            if (reason == "version")
                await db.Users.Where(value => value.Id == account.Id).ExecuteUpdateAsync(update => update.SetProperty(value => value.SecurityVersion, 2));
            if (reason == "revoked-session")
                await db.Sessions.Where(value => value.Id == credential.SessionId).ExecuteUpdateAsync(update => update.SetProperty(value => value.RevokedAt, DateTimeOffset.UtcNow));
            if (reason == "revoked-credential")
                await db.RefreshCredentials.Where(value => value.Id == credential.Id).ExecuteUpdateAsync(update => update.SetProperty(value => value.RevokedAt, DateTimeOffset.UtcNow));
            if (reason is "session-expired" or "credential-expired")
            {
                var now = DateTimeOffset.UtcNow;
                await db.RefreshCredentials.Where(value => value.Id == credential.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(value => value.CreatedAt, now.AddHours(-1))
                        .SetProperty(value => value.ExpiresAt, now.AddSeconds(-1)));
                if (reason == "session-expired")
                    await db.Sessions.Where(value => value.Id == credential.SessionId)
                        .ExecuteUpdateAsync(update => update.SetProperty(value => value.CreatedAt, now.AddHours(-1))
                            .SetProperty(value => value.ExpiresAt, now.AddSeconds(-1)));
            }
            if (reason == "profile") await db.Profiles.Where(value => value.UserId == account.Id).ExecuteDeleteAsync();
            if (reason == "membership") await db.UserRoles.Where(value => value.UserId == account.Id).ExecuteDeleteAsync();
        }
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(response, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
    }

    [Fact]
    public async Task RefreshLifetimeIsClampedToAbsoluteSessionEnd()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account, remember: true);
        var raw = Raw(login);
        SetRefresh(client, raw);
        var end = DateTimeOffset.UtcNow.AddSeconds(45);
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var hash = RefreshSecrets.TryHash(raw)!;
            var credential = await db.RefreshCredentials.SingleAsync(value => value.TokenHash == hash);
            await db.Sessions.Where(value => value.Id == credential.SessionId).ExecuteUpdateAsync(update => update.SetProperty(value => value.ExpiresAt, end));
            await db.RefreshCredentials.Where(value => value.Id == credential.Id).ExecuteUpdateAsync(update => update.SetProperty(value => value.ExpiresAt, end));
        }
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.InRange(json.RootElement.GetProperty("data").GetProperty("expiresIn").GetInt32(), 1, 45);
        Assert.True(json.RootElement.GetProperty("data").GetProperty("expiresAt").GetDateTime() <= end.UtcDateTime);
    }

    [Fact]
    public async Task RotationPreservesCredentialExpiryWhenItIsEarlierThanSessionExpiry()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account, remember: true);
        var raw = Raw(login);
        SetRefresh(client, raw);
        var end = DateTimeOffset.UtcNow.AddMinutes(5);
        await using (var setup = database.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var hash = RefreshSecrets.TryHash(raw)!;
            await db.RefreshCredentials.Where(value => value.TokenHash == hash)
                .ExecuteUpdateAsync(update => update.SetProperty(value => value.ExpiresAt, end));
        }
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("max-age=", Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant());
        var successorHash = RefreshSecrets.TryHash(Raw(response))!;
        await using var verification = database.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var successor = await context.RefreshCredentials.SingleAsync(value => value.TokenHash == successorHash);
        var previousHash = RefreshSecrets.TryHash(raw)!;
        var previous = await context.RefreshCredentials.SingleAsync(value => value.TokenHash == previousHash);
        Assert.Equal(previous.ExpiresAt, successor.ExpiresAt);
        Assert.True(successor.ExpiresAt < (await context.Sessions.SingleAsync(value => value.Id == successor.SessionId)).ExpiresAt);
    }

    [Fact]
    public async Task PasswordLockoutDoesNotInvalidateExistingRefreshSession()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account);
        SetRefresh(client, Raw(login));
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Users.Where(value => value.Id == account.Id).ExecuteUpdateAsync(update =>
                update.SetProperty(value => value.LockoutEnd, DateTimeOffset.UtcNow.AddMinutes(15)));
        }
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SessionQuotaCountsSuccessiveRotationsWithoutResettingForNewToken()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account);
        var raw = Raw(login);
        for (var index = 0; index < 30; index++)
        {
            SetRefresh(client, raw);
            using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            raw = Raw(response);
        }
        SetRefresh(client, raw);
        using var rejected = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(rejected, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }
}
