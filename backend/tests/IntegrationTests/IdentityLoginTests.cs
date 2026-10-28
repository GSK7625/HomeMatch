using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

public partial class IdentityLoginTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "Mật khẩu kiểm thử đăng nhập 2026!";
    private readonly string _secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private ApiFactory Factory(bool available = true, string environment = "Production") => new(new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = _secret,
        ["ConnectionStrings:DefaultConnection"] = available ? database.ConnectionString
            : "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1;Command Timeout=1"
    }, environment);

    private static async Task<HttpClient> BrowserAsync(WebApplicationFactory<Program> factory, string url = "https://api.example.test")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            { BaseAddress = new Uri(url), HandleCookies = false, AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using var csrf = await client.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(csrf.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        using var body = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", body.RootElement.GetProperty("data").GetProperty("requestToken").GetString());
        return client;
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password = Password, bool remember = false)
        => client.PostAsJsonAsync("/api/v1/identity/auth/login", new { email, password, rememberMe = remember });

    private static async Task<string> AccessToken(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private static string RefreshSecret(HttpResponseMessage response)
        => Assert.Single(response.Headers.GetValues("Set-Cookie")).Split(';')[0].Split('=', 2)[1];

    private static void SetRefresh(HttpClient client, string raw, string name = "__Secure-HM.Refresh")
    {
        var anti = client.DefaultRequestHeaders.GetValues("Cookie").Single().Split(';')[0];
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", anti + "; " + name + "=" + raw);
    }

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode expected, string code)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("error").GetProperty("code").GetString());
        if (expected == HttpStatusCode.Unauthorized) Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    private async Task<IdentityAccount> AccountAsync(string? email = null)
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = new IdentityAccount(email ?? $"login-{Guid.NewGuid():N}@example.test", DateTimeOffset.UtcNow);
        Assert.True((await manager.CreateAsync(account, Password)).Succeeded);
        Assert.True((await manager.AddToRoleAsync(account, "Tenant")).Succeeded);
        context.Profiles.Add(new UserProfile(account.Id, "Nguyễn Văn An", account.CreatedAt));
        await context.SaveChangesAsync();
        return account;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisterLoginMeWorksBeforeEmailVerificationAndStoresOnlyRefreshHash(bool remember)
    {
        var email = $"flow-{Guid.NewGuid():N}@example.test";
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var registered = await client.PostAsJsonAsync("/api/v1/identity/auth/register",
            new { email, password = Password, fullName = "Nguyễn Văn An", role = remember ? "Landlord" : "Tenant" });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "stale-token");
        using var login = await Login(client, email.ToUpperInvariant(), remember: remember);
        var token = await AccessToken(login);
        var raw = RefreshSecret(login);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", raw);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.StartsWith("__secure-hm.refresh=", cookie);
        Assert.Contains("path=/api/v1/identity", cookie);
        Assert.Contains("; secure", cookie);
        Assert.Contains("; httponly", cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.DoesNotContain("domain=", cookie);
        Assert.Equal(remember, cookie.Contains("expires="));
        Assert.Equal(remember, cookie.Contains("max-age="));
        var json = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain(raw, json);
        using var parsed = JsonDocument.Parse(json);
        var data = parsed.RootElement.GetProperty("data");
        Assert.Equal(new[] { "accessToken", "expiresAt", "expiresIn", "sessionExpiresAt", "tokenType" },
            data.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal("Bearer", data.GetProperty("tokenType").GetString());
        Assert.InRange(data.GetProperty("expiresIn").GetInt32(), 1, 900);
        var end = data.GetProperty("sessionExpiresAt").GetDateTime();
        Assert.InRange(end - DateTime.UtcNow, remember ? TimeSpan.FromDays(29) : TimeSpan.FromHours(23),
            remember ? TimeSpan.FromDays(30) : TimeSpan.FromHours(24));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var profile = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.False(profile.RootElement.GetProperty("data").GetProperty("emailVerified").GetBoolean());
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var account = await db.Users.SingleAsync(value => value.NormalizedEmail == email.ToUpperInvariant());
        var session = await db.Sessions.SingleAsync(value => value.UserId == account.Id);
        var credential = await db.RefreshCredentials.SingleAsync(value => value.SessionId == session.Id);
        Assert.Equal(remember, session.RememberMe);
        Assert.Equal(RefreshSecrets.TryHash(raw), credential.TokenHash);
        Assert.Equal(session.ExpiresAt, credential.ExpiresAt);
        Assert.Null(credential.UsedAt);
        Assert.Null(credential.RevokedAt);
    }

    [Fact]
    public async Task DevelopmentHttpUsesDevelopmentRefreshCookie()
    {
        var account = await AccountAsync();
        await using var factory = Factory(environment: "Development");
        using var client = await BrowserAsync(factory, "http://localhost");
        using var response = await Login(client, account.Email!);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.StartsWith("hm.refresh.dev=", cookie);
        Assert.DoesNotContain("; secure", cookie);
        Assert.Contains("; httponly", cookie);
    }

    [Theory]
    [InlineData("old")]
    [InlineData("123456789012345")]
    public async Task LegacyPasswordCanLoginWithoutNewCreationPolicy(string old)
    {
        var account = await AccountAsync();
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var stored = await db.Users.SingleAsync(value => value.Id == account.Id);
            stored.PasswordHash = new PasswordHasher<IdentityAccount>().HashPassword(stored, old);
            await db.SaveChangesAsync();
        }
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var response = await Login(client, account.Email!, old);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
