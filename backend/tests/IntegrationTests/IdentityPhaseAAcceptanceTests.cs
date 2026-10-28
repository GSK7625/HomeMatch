using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

/// <summary>Acceptance stories use the public HTTP API and a cookie jar; database reads only verify final invariants.</summary>
public sealed class IdentityPhaseAAcceptanceTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Root = "/api/v1/identity";
    private const string Password = "Mật khẩu nghiệm thu tài khoản 2026!";
    private const string FullName = "Nguyễn Văn An";

    private ApiFactory Factory() => new(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = database.ConnectionString
    });

    [Theory]
    [InlineData("Tenant", false)]
    [InlineData("Tenant", true)]
    [InlineData("Landlord", false)]
    [InlineData("Landlord", true)]
    public async Task UnverifiedAccountCompletesRegistrationAndSessionLifecycleAcrossDevices(string role, bool remember)
    {
        var email = $"acceptance-{Guid.NewGuid():N}@example.test";
        await using var factory = Factory();
        using var first = await Browser.CreateAsync(factory);
        using var second = await Browser.CreateAsync(factory);

        // Registration must neither create a session nor allow a duplicate request to overwrite the account.
        using (var registered = await first.SendAsync(HttpMethod.Post, "/auth/register",
                   new { email, password = Password, fullName = FullName, role }))
        {
            Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
            Assert.False(registered.Headers.Contains("Set-Cookie"));
        }
        await AssertProfileAsync(first, null, HttpStatusCode.Unauthorized);
        using (var duplicate = await first.SendAsync(HttpMethod.Post, "/auth/register",
                   new { email = email.ToUpperInvariant(), password = "Một mật khẩu khác khi đăng ký 2026!",
                       fullName = "Tên không được ghi đè", role = role == "Tenant" ? "Landlord" : "Tenant" }))
            Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);

        var initial = await LoginAsync(first, email.ToUpperInvariant(), remember);
        var otherDevice = await LoginAsync(second, email, !remember);
        var owner = await AssertProfileAsync(first, initial.AccessToken, HttpStatusCode.OK, email, role);
        Assert.Equal(owner, await AssertProfileAsync(second, otherDevice.AccessToken, HttpStatusCode.OK, email, role));

        Session rotated;
        using (var refresh = await first.SendAsync(HttpMethod.Post, "/auth/refresh"))
            rotated = await ReadSessionAsync(first, refresh, remember);
        Assert.NotEqual(initial.RefreshSecret, rotated.RefreshSecret);
        Assert.Equal(initial.SessionExpiresAt, rotated.SessionExpiresAt);
        await AssertProfileAsync(first, rotated.AccessToken, HttpStatusCode.OK, email, role);

        // The cookie jar applies Set-Cookie deletion, while saved JWTs prove revocation on the server.
        using (var logout = await first.SendAsync(HttpMethod.Post, "/auth/logout", accessToken: "stale-token"))
            await AssertNoContentAsync(logout);
        Assert.Null(first.RefreshCookie);
        await AssertProfileAsync(first, initial.AccessToken, HttpStatusCode.Unauthorized);
        await AssertProfileAsync(first, rotated.AccessToken, HttpStatusCode.Unauthorized);
        await AssertProfileAsync(second, otherDevice.AccessToken, HttpStatusCode.OK, email, role);
        first.RestoreRefreshCookie(rotated.RefreshSecret);
        using (var revokedRefresh = await first.SendAsync(HttpMethod.Post, "/auth/refresh"))
            Assert.Equal(HttpStatusCode.Unauthorized, revokedRefresh.StatusCode);

        var signedInAgain = await LoginAsync(first, email, remember);
        using (var logoutAll = await second.SendAsync(HttpMethod.Post, "/auth/logout-all",
                   accessToken: otherDevice.AccessToken))
            await AssertNoContentAsync(logoutAll);
        Assert.Null(second.RefreshCookie);
        await AssertProfileAsync(first, signedInAgain.AccessToken, HttpStatusCode.Unauthorized);
        await AssertProfileAsync(second, otherDevice.AccessToken, HttpStatusCode.Unauthorized);
        using (var revokedRefresh = await first.SendAsync(HttpMethod.Post, "/auth/refresh"))
            Assert.Equal(HttpStatusCode.Unauthorized, revokedRefresh.StatusCode);

        // Logout-all does not suspend the account: a fresh password login uses the new security version.
        var fresh = await LoginAsync(second, email, !remember);
        await AssertProfileAsync(second, fresh.AccessToken, HttpStatusCode.OK, email, role);

        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var account = await db.Users.AsNoTracking().SingleAsync(value => value.NormalizedEmail == email.ToUpperInvariant());
        Assert.Equal(owner, account.Id);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.False(account.EmailConfirmed);
        Assert.Equal(2L, account.SecurityVersion);
        var sessions = await db.Sessions.AsNoTracking().Where(value => value.UserId == owner).ToListAsync();
        Assert.Equal(4, sessions.Count);
        Assert.Equal(3, sessions.Count(value => value.RevokedAt is not null));
        var live = Assert.Single(sessions, value => value.RevokedAt is null);
        Assert.Equal(account.SecurityVersion, live.SecurityVersion);
        var credentials = await db.RefreshCredentials.AsNoTracking()
            .Where(value => db.Sessions.Any(parent => parent.Id == value.SessionId && parent.UserId == owner)).ToListAsync();
        Assert.Equal(5, credentials.Count);
        Assert.All(credentials.Where(value => value.SessionId != live.Id), value => Assert.NotNull(value.RevokedAt));
        Assert.Single(credentials, value => value.UsedAt is null && value.RevokedAt is null);
    }

    [Theory]
    [InlineData("/auth/logout-all/")]
    [InlineData("/AUTH/LOGOUT-ALL")]
    [InlineData("/AUTH/LOGOUT-ALL/")]
    public async Task LogoutAllUsesEndpointAuthorizationForEquivalentRoutePaths(string route)
    {
        var email = $"route-{Guid.NewGuid():N}@example.test";
        await using var factory = Factory();
        using var browser = await Browser.CreateAsync(factory);
        using (var registered = await browser.SendAsync(HttpMethod.Post, "/auth/register",
                   new { email, password = Password, fullName = FullName, role = "Tenant" }))
            Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var session = await LoginAsync(browser, email, false);

        using (var denied = await browser.SendAsync(HttpMethod.Post, route, accessToken: "invalid-token"))
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await AssertProfileAsync(browser, session.AccessToken, HttpStatusCode.OK);

        using (var logout = await browser.SendAsync(HttpMethod.Post, route, accessToken: session.AccessToken))
            await AssertNoContentAsync(logout);
        await AssertProfileAsync(browser, session.AccessToken, HttpStatusCode.Unauthorized);
    }

    private static async Task<Session> LoginAsync(Browser browser, string email, bool remember)
    {
        using var response = await browser.SendAsync(HttpMethod.Post, "/auth/login",
            new { email, password = Password, rememberMe = remember });
        return await ReadSessionAsync(browser, response, remember);
    }

    private static async Task<Session> ReadSessionAsync(Browser browser, HttpResponseMessage response, bool remember)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await ReadDataAsync(response);
        var cookie = Assert.IsType<Cookie>(browser.RefreshCookie);
        Assert.Equal(remember, cookie.Expires != DateTime.MinValue);
        Assert.True(cookie.Secure);
        Assert.True(cookie.HttpOnly);
        Assert.DoesNotContain(cookie.Value, await response.Content.ReadAsStringAsync());
        return new Session(data.GetProperty("accessToken").GetString()!, cookie.Value,
            data.GetProperty("sessionExpiresAt").GetDateTimeOffset());
    }

    private static async Task<Guid> AssertProfileAsync(Browser browser, string? accessToken, HttpStatusCode status,
        string? email = null, string? role = null)
    {
        using var response = await browser.SendAsync(HttpMethod.Get, "/me", accessToken: accessToken);
        Assert.Equal(status, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        if (status != HttpStatusCode.OK)
        {
            Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
            return Guid.Empty;
        }
        var data = await ReadDataAsync(response);
        Assert.False(data.GetProperty("emailVerified").GetBoolean());
        Assert.Equal(FullName, data.GetProperty("fullName").GetString());
        if (email is not null) Assert.Equal(email, data.GetProperty("email").GetString());
        if (role is not null) Assert.Equal(role, data.GetProperty("role").GetString());
        return data.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("isSuccess").GetBoolean());
        return json.RootElement.GetProperty("data").Clone();
    }

    private static async Task AssertNoContentAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    private sealed record Session(string AccessToken, string RefreshSecret, DateTimeOffset SessionExpiresAt);

    private sealed class Browser(HttpClient client) : IDisposable
    {
        private static readonly Uri Api = new("https://api.example.test" + Root + "/");
        private readonly CookieContainer _cookies = new();
        private string? _csrf;
        public Cookie? RefreshCookie => _cookies.GetCookies(Api)["__Secure-HM.Refresh"];

        public static async Task<Browser> CreateAsync(WebApplicationFactory<Program> factory)
        {
            var browser = new Browser(factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://api.example.test"), HandleCookies = false, AllowAutoRedirect = false
            }));
            try
            {
                using var response = await browser.SendAsync(HttpMethod.Get, "/auth/csrf");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                browser._csrf = (await ReadDataAsync(response)).GetProperty("requestToken").GetString();
                return browser;
            }
            catch
            {
                browser.Dispose();
                throw;
            }
        }

        public void RestoreRefreshCookie(string secret) => _cookies.Add(Api,
            new Cookie("__Secure-HM.Refresh", secret, Root) { Secure = true, HttpOnly = true });

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, object? body = null,
            string? accessToken = null)
        {
            using var request = new HttpRequestMessage(method, Root + route);
            var uri = new Uri(client.BaseAddress!, Root + route);
            request.Headers.Add("Origin", "https://frontend.example.test");
            if (_csrf is not null) request.Headers.Add("X-CSRF-TOKEN", _csrf);
            var cookies = _cookies.GetCookieHeader(uri);
            if (cookies.Length > 0) request.Headers.Add("Cookie", cookies);
            if (accessToken is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (body is not null) request.Content = JsonContent.Create(body);
            var response = await client.SendAsync(request);
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                foreach (var value in setCookies) _cookies.SetCookies(uri, value);
            return response;
        }

        public void Dispose() => client.Dispose();
    }
}
