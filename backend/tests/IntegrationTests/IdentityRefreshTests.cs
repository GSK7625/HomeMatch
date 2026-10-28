using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HomeMatch.IntegrationTests;

public partial class IdentityRefreshTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "Mật khẩu dùng thử refresh 2026!";
    private readonly string _secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private ApiFactory Factory(bool available = true, string environment = "Production") => new(new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = _secret,
        ["ConnectionStrings:DefaultConnection"] = available ? database.ConnectionString
            : "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1;Command Timeout=1"
    }, environment);

    private static async Task<HttpClient> Browser(WebApplicationFactory<Program> factory, string url = "https://api.example.test")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            { BaseAddress = new Uri(url), HandleCookies = false, AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using var csrf = await client.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(csrf.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        using var json = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", json.RootElement.GetProperty("data").GetProperty("requestToken").GetString());
        return client;
    }

    private async Task<IdentityAccount> Account()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = new IdentityAccount(Guid.NewGuid().ToString("N") + "@example.test", DateTimeOffset.UtcNow);
        Assert.True((await manager.CreateAsync(account, Password)).Succeeded);
        Assert.True((await manager.AddToRoleAsync(account, "Tenant")).Succeeded);
        db.Profiles.Add(new UserProfile(account.Id, "Nguyễn Văn An", account.CreatedAt));
        await db.SaveChangesAsync();
        return account;
    }

    private static string Raw(HttpResponseMessage response)
        => Assert.Single(response.Headers.GetValues("Set-Cookie")).Split(';')[0].Split('=', 2)[1];

    private static void SetRefresh(HttpClient client, string raw, string name = "__Secure-HM.Refresh")
    {
        var anti = client.DefaultRequestHeaders.GetValues("Cookie").Single().Split(';')[0];
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", anti + "; " + name + "=" + raw);
    }

    private static async Task<string> Access(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> Login(HttpClient client, IdentityAccount account, bool remember = false)
        => await client.PostAsJsonAsync("/api/v1/identity/auth/login", new { email = account.Email, password = Password, rememberMe = remember });

    private static async Task AssertFailure(HttpResponseMessage response, HttpStatusCode expected, string code, bool deletesCookie = false)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(deletesCookie, response.Headers.Contains("Set-Cookie"));
        if (deletesCookie)
        {
            var header = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
            Assert.Contains("max-age=0", header);
            Assert.Contains("path=/api/v1/identity", header);
            Assert.Contains("httponly", header);
            Assert.DoesNotContain("domain=", header);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RotationPreservesSessionAbsoluteExpiryAndCookiePersistence(bool remember)
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account, remember);
        var oldRaw = Raw(login);
        var oldAccess = await Access(login);
        var oldJwt = new JsonWebTokenHandler().ReadJsonWebToken(oldAccess);
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var absoluteEnd = loginJson.RootElement.GetProperty("data").GetProperty("sessionExpiresAt").GetDateTime();
        SetRefresh(client, oldRaw);
        // Stale Bearer must not be the identity for this cookie operation.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "expired-or-foreign-token");
        using var refreshed = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        var newRaw = Raw(refreshed);
        var newAccess = await Access(refreshed);
        Assert.NotEqual(oldRaw, newRaw);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(newAccess);
        Assert.Equal(oldJwt.GetClaim("sid").Value, jwt.GetClaim("sid").Value);
        Assert.NotEqual(oldJwt.Id, jwt.Id);
        var cookie = Assert.Single(refreshed.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.Contains("; secure", cookie);
        Assert.Equal(remember, cookie.Contains("expires="));
        Assert.Equal(remember, cookie.Contains("max-age="));
        using var resultJson = JsonDocument.Parse(await refreshed.Content.ReadAsStringAsync());
        Assert.Equal(absoluteEnd, resultJson.RootElement.GetProperty("data").GetProperty("sessionExpiresAt").GetDateTime());
        Assert.DoesNotContain(newRaw, await refreshed.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAccess);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var oldHash = RefreshSecrets.TryHash(oldRaw)!;
        var old = await db.RefreshCredentials.SingleAsync(value => value.TokenHash == oldHash);
        var next = await db.RefreshCredentials.SingleAsync(value => value.Id == old.ReplacedByCredentialId);
        Assert.NotNull(old.UsedAt);
        Assert.Equal(old.SessionId, next.SessionId);
        Assert.Equal(old.ExpiresAt, next.ExpiresAt);
        Assert.Equal(RefreshSecrets.TryHash(newRaw), next.TokenHash);
        Assert.Null(next.UsedAt);
        Assert.Null(next.RevokedAt);
    }

    [Fact]
    public async Task ReplayingOlderTokenRevokesWholeFamilyButLeavesOtherDeviceUsable()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var otherDevice = await Browser(factory);
        using var otherLogin = await Login(otherDevice, account);
        var otherAccess = await Access(otherLogin);
        using var login = await Login(client, account);
        var original = Raw(login);
        SetRefresh(client, original);
        using var rotated = await client.PostAsJsonAsync("/api/v1/identity/auth/refresh", new { });
        var winnerAccess = await Access(rotated);
        var winnerRaw = Raw(rotated);
        using var replay = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(replay, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", winnerAccess);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        SetRefresh(client, winnerRaw);
        using var newTokenAlsoInvalid = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(newTokenAlsoInvalid, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
        otherDevice.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherAccess);
        using var unaffected = await otherDevice.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, unaffected.StatusCode);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var oldHash = RefreshSecrets.TryHash(original)!;
        var session = await db.RefreshCredentials.Where(value => value.TokenHash == oldHash).Select(value => value.SessionId).SingleAsync();
        var history = await db.RefreshCredentials.Where(value => value.SessionId == session).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.All(history, value => Assert.NotNull(value.RevokedAt));
        Assert.NotNull((await db.Sessions.SingleAsync(value => value.Id == session)).RevokedAt);
    }
}
