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
using Microsoft.IdentityModel.Tokens;

namespace HomeMatch.IntegrationTests;

public partial class IdentityLogoutTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "Mật khẩu kiểm thử logout 2026!";
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
        using var response = await client.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(response.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
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

    private static async Task<(string Access, string Raw)> Login(HttpClient client, IdentityAccount account)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/login",
            new { email = account.Email, password = Password, rememberMe = false });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!,
            Assert.Single(response.Headers.GetValues("Set-Cookie")).Split(';')[0].Split('=', 2)[1]);
    }

    private static void SetRefresh(HttpClient client, string raw, string name = "__Secure-HM.Refresh")
    {
        var anti = client.DefaultRequestHeaders.GetValues("Cookie").Single().Split(';')[0];
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", anti + "; " + name + "=" + raw);
    }

    private static async Task AssertNoContent(HttpResponseMessage response, bool deletesCookie)
    {
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal(deletesCookie, response.Headers.Contains("Set-Cookie"));
        if (deletesCookie)
        {
            var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
            Assert.Contains("max-age=0", cookie);
            Assert.Contains("path=/api/v1/identity", cookie);
            Assert.Contains("httponly", cookie);
            Assert.DoesNotContain("domain=", cookie);
        }
    }

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        if (status == HttpStatusCode.Unauthorized) Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    private static async Task AssertMe(HttpClient client, string access, HttpStatusCode status)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(status, me.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("malformed")]
    [InlineData("unknown")]
    public async Task CurrentLogoutIsIdempotentForMissingOrInvalidCookie(string? kind)
    {
        await using var factory = Factory();
        using var client = await Browser(factory);
        if (kind is not null) SetRefresh(client, kind == "unknown" ? RefreshSecrets.Create().Raw : "bad");
        using var first = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertNoContent(first, true);
        using var repeated = await client.PostAsJsonAsync("/api/v1/identity/auth/logout", new { });
        await AssertNoContent(repeated, true);
    }

    [Fact]
    public async Task CurrentLogoutWorksWithExpiredJwtAndDoesNotTouchAnotherDevice()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var other = await Browser(factory);
        var first = await Login(client, account);
        var second = await Login(other, account);
        SetRefresh(client, first.Raw);
        var claims = new JsonWebTokenHandler().ReadJsonWebToken(first.Access).Claims
            .Where(claim => claim.Type is "sub" or "sid" or "role" or "sv").ToDictionary(claim => claim.Type, claim => (object)claim.Value);
        claims["jti"] = Guid.NewGuid().ToString("D");
        var expired = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "HomeMatch.Tests", Audience = "HomeMatch.Tests.Client",
            IssuedAt = DateTime.UtcNow.AddMinutes(-10), NotBefore = DateTime.UtcNow.AddMinutes(-10),
            Expires = DateTime.UtcNow.AddMinutes(-1), Claims = claims,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(_secret)), SecurityAlgorithms.HmacSha256)
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);
        using var logout = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertNoContent(logout, true);
        await AssertMe(client, first.Access, HttpStatusCode.Unauthorized);
        await AssertMe(other, second.Access, HttpStatusCode.OK);
        using var again = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertNoContent(again, true);
    }

    [Fact]
    public async Task CurrentLogoutCanRevokeFamilyUsingAConsumedRefreshToken()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var login = await Login(client, account);
        SetRefresh(client, login.Raw);
        using var refresh = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        using var json = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
        var latest = json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        using var logout = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertNoContent(logout, true);
        await AssertMe(client, latest, HttpStatusCode.Unauthorized);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var family = await db.Sessions.SingleAsync(value => value.UserId == account.Id);
        Assert.NotNull(family.RevokedAt);
        Assert.All(await db.RefreshCredentials.Where(value => value.SessionId == family.Id).ToListAsync(),
            value => Assert.NotNull(value.RevokedAt));
    }

    [Fact]
    public async Task LogoutAllRevokesEverySessionAndBumpsVersionOnce()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var other = await Browser(factory);
        var first = await Login(client, account);
        var second = await Login(other, account);
        SetRefresh(client, first.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.Access);
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var response = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertNoContent(response, true);
        await AssertMe(client, first.Access, HttpStatusCode.Unauthorized);
        await AssertMe(other, second.Access, HttpStatusCode.Unauthorized);
        SetRefresh(other, second.Raw);
        using var refresh = await other.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        using var repeated = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertError(repeated, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(2, (await db.Users.SingleAsync(value => value.Id == account.Id)).SecurityVersion);
        Assert.All(await db.Sessions.Where(value => value.UserId == account.Id).ToListAsync(), value => Assert.NotNull(value.RevokedAt));
        Assert.All(await db.RefreshCredentials.Where(value => db.Sessions.Any(session =>
            session.Id == value.SessionId && session.UserId == account.Id)).ToListAsync(), value => Assert.NotNull(value.RevokedAt));
    }

    [Fact]
    public async Task LogoutAllWithOldAccountBearerPreservesOtherAccountsCookieAndSession()
    {
        var first = await Account();
        var other = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var firstLogin = await Login(client, first);
        var otherLogin = await Login(client, other);
        SetRefresh(client, otherLogin.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstLogin.Access);
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/logout-all", new { });
        await AssertNoContent(response, false);
        await AssertMe(client, firstLogin.Access, HttpStatusCode.Unauthorized);
        await AssertMe(client, otherLogin.Access, HttpStatusCode.OK);
        using var refreshOther = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refreshOther.StatusCode);
    }

    [Fact]
    public async Task CurrentLogoutUsesCookieIdentityEvenWhenBearerBelongsToAnotherAccount()
    {
        var first = await Account();
        var other = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var firstLogin = await Login(client, first);
        var otherLogin = await Login(client, other);
        SetRefresh(client, otherLogin.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstLogin.Access);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertNoContent(response, true);
        await AssertMe(client, firstLogin.Access, HttpStatusCode.OK);
        await AssertMe(client, otherLogin.Access, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LogoutAllWithoutAnOwnedCookieDoesNotDeleteAnUnknownCookie(bool unknown)
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var login = await Login(client, account);
        if (unknown) SetRefresh(client, RefreshSecrets.Create().Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertNoContent(response, false);
        await AssertMe(client, login.Access, HttpStatusCode.Unauthorized);
    }
}
