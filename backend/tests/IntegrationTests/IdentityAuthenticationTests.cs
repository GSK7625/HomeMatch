using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HomeMatch.IntegrationTests;

public class IdentityAuthenticationTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private readonly string _secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private ApiFactory Factory(bool realDatabase = true, string environment = "Production", int clockSkewSeconds = 0) => new(
        new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = _secret,
            ["Jwt:ClockSkewSeconds"] = clockSkewSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ConnectionStrings:DefaultConnection"] = realDatabase ? database.ConnectionString
                : "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1;Command Timeout=1"
        }, environment);

    private static HttpClient Client(ApiFactory factory, string origin = "https://api.example.test") => factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri(origin), HandleCookies = false, AllowAutoRedirect = false });

    private async Task<(Guid UserId, AccountSession Session)> NewAccountAsync(string role = AccountRoles.Tenant,
        bool verified = false, DateTimeOffset? expires = null)
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = new IdentityAccount($"auth-{Guid.NewGuid():N}@example.test", DateTimeOffset.UtcNow.AddMinutes(-1))
            { EmailConfirmed = verified };
        Assert.True((await manager.CreateAsync(account, "Mat khau integration test 2026!")).Succeeded);
        Assert.True((await manager.AddToRoleAsync(account, role)).Succeeded);
        context.Profiles.Add(new UserProfile(account.Id, "Nguyễn Văn An", account.CreatedAt));
        var session = new AccountSession(account.Id, account.SecurityVersion, false,
            account.CreatedAt, expires ?? DateTimeOffset.UtcNow.AddDays(1));
        context.Sessions.Add(session);
        await context.SaveChangesAsync();
        return (account.Id, session);
    }

    private string Token(ApiFactory factory, Guid userId, AccountSession session, string role = AccountRoles.Tenant)
        => factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(
            new AccessTokenSubject(userId, session.Id, role, session.SecurityVersion, session.ExpiresAt)).AccessToken;

    private string CustomToken(Guid userId, Guid sessionId, string invalid = "", DateTime? expiredSessionEnd = null)
    {
        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["sub"] = userId.ToString("D"), ["sid"] = sessionId.ToString("D"),
            ["jti"] = Guid.NewGuid().ToString("D"), ["role"] = AccountRoles.Tenant, ["sv"] = "1"
        };
        if (invalid == "sid") claims.Remove("sid");
        if (invalid == "jti") claims.Remove("jti");
        if (invalid == "guid") claims["sub"] = "not-a-guid";
        if (invalid == "version") claims["sv"] = "0";
        if (invalid == "role") claims["role"] = "admin";
        if (invalid == "roles") claims["role"] = new[] { AccountRoles.Tenant, AccountRoles.Admin };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = invalid == "issuer" ? "wrong" : "HomeMatch.Tests",
            Audience = invalid == "audience" ? "wrong" : "HomeMatch.Tests.Client",
            IssuedAt = expiredSessionEnd?.AddMinutes(-15) ?? (invalid == "expired" ? now.AddMinutes(-20) : now),
            NotBefore = expiredSessionEnd?.AddMinutes(-15) ?? (invalid == "expired" ? now.AddMinutes(-20) : invalid == "future" ? now.AddMinutes(2) : now),
            Expires = expiredSessionEnd ?? (invalid == "expired" ? now.AddMinutes(-5) : invalid == "long" ? now.AddMinutes(16) : now.AddMinutes(15)),
            Claims = claims,
            TokenType = invalid == "type" ? "OTHER" : "JWT",
            SigningCredentials = invalid == "none" ? null : new SigningCredentials(
                new SymmetricSecurityKey(invalid == "key" ? RandomNumberGenerator.GetBytes(64) : Convert.FromBase64String(_secret)),
                invalid == "algorithm" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("data").ValueKind);
        Assert.Equal(code, body.RootElement.GetProperty("error").GetProperty("code").GetString());
        if (status == HttpStatusCode.Unauthorized)
            Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MeReturnsOnlyOwnProfileAndRealVerificationState(bool verified)
    {
        var (userId, session) = await NewAccountAsync(verified: verified);
        await using var factory = Factory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, userId, session));
        using var response = await client.GetAsync("/api/v1/identity/me?userId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal(userId, data.GetProperty("id").GetGuid());
        Assert.Equal(verified, data.GetProperty("emailVerified").GetBoolean());
        Assert.Equal(AccountRoles.Tenant, data.GetProperty("role").GetString());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("phoneNumber").ValueKind);
        Assert.Equal(new[] { "createdAt", "email", "emailVerified", "fullName", "id", "phoneNumber", "role", "updatedAt" },
            data.EnumerateObject().Select(value => value.Name).Order());
        Assert.Equal(DateTimeKind.Utc, data.GetProperty("createdAt").GetDateTime().Kind);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("key")]
    [InlineData("algorithm")]
    [InlineData("none")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("sid")]
    [InlineData("jti")]
    [InlineData("guid")]
    [InlineData("version")]
    [InlineData("role")]
    [InlineData("roles")]
    [InlineData("type")]
    [InlineData("long")]
    public async Task InvalidBearerCannotReachProtectedEndpoint(string invalid)
    {
        // Unreachable DB proves cryptographically/structurally invalid tokens fail before store access.
        await using var factory = Factory(realDatabase: false);
        using var client = Client(factory);
        if (invalid != "missing")
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                invalid == "malformed" ? "private-invalid-token" : CustomToken(Guid.NewGuid(), Guid.NewGuid(), invalid));
        using var response = await client.GetAsync("/api/v1/identity/me");
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
        Assert.DoesNotContain("private-invalid-token", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("suspended")]
    [InlineData("account-version")]
    [InlineData("session-version")]
    [InlineData("role")]
    [InlineData("profile")]
    [InlineData("deleted")]
    public async Task DatabaseStateChangeRejectsPreviouslyValidAccessToken(string change)
    {
        var (userId, session) = await NewAccountAsync();
        await using var factory = Factory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, userId, session));
        using (var before = await client.GetAsync("/api/v1/identity/me")) Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            if (change == "revoked") (await context.Sessions.SingleAsync(value => value.Id == session.Id)).Revoke(DateTimeOffset.UtcNow);
            if (change == "session-version")
                await context.Sessions.Where(value => value.Id == session.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.SecurityVersion, 2));
            var account = await context.Users.SingleAsync(value => value.Id == userId);
            if (change == "suspended") account.Status = AccountStatus.Suspended;
            if (change == "account-version") account.SecurityVersion++;
            if (change == "role")
            {
                var membership = await context.UserRoles.SingleAsync(value => value.UserId == userId);
                context.UserRoles.Remove(membership);
                await context.SaveChangesAsync();
                context.UserRoles.Add(new IdentityUserRole<Guid>
                {
                    UserId = userId, RoleId = await context.Roles.Where(value => value.Name == AccountRoles.Admin).Select(value => value.Id).SingleAsync()
                });
            }
            if (change == "profile") context.Profiles.Remove(await context.Profiles.SingleAsync(value => value.UserId == userId));
            if (change == "deleted") context.Users.Remove(account);
            await context.SaveChangesAsync();
        }
        using var response = await client.GetAsync("/api/v1/identity/me");
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
    }

    [Fact]
    public async Task SessionExpiryHasNoJwtClockSkewGrace()
    {
        var (userId, session) = await NewAccountAsync(expires: DateTimeOffset.UtcNow.AddSeconds(-1));
        await using var factory = Factory(clockSkewSeconds: 30);
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CustomToken(userId, session.Id, expiredSessionEnd: session.ExpiresAt.UtcDateTime));
        using var response = await client.GetAsync("/api/v1/identity/me");
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
    }

    [Fact]
    public async Task TokenCannotUseAnotherAccountsSession()
    {
        var (firstId, _) = await NewAccountAsync();
        var (_, secondSession) = await NewAccountAsync();
        await using var factory = Factory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, firstId, secondSession));
        using var response = await client.GetAsync("/api/v1/identity/me");
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
    }

    [Fact]
    public async Task PasswordLockoutDoesNotRevokeExistingSession()
    {
        var (userId, session) = await NewAccountAsync();
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var account = await context.Users.SingleAsync(value => value.Id == userId);
            account.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            await context.SaveChangesAsync();
        }
        await using var factory = Factory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, userId, session));
        using var response = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnavailableAuthenticationStoreReturnsGeneric503()
    {
        await using var factory = Factory(realDatabase: false);
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CustomToken(Guid.NewGuid(), Guid.NewGuid()));
        using var response = await client.GetAsync("/api/v1/identity/me");
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
        Assert.DoesNotContain("Npgsql", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(AccountRoles.Tenant, HttpStatusCode.Forbidden)]
    [InlineData(AccountRoles.Admin, HttpStatusCode.NoContent)]
    public async Task RolePolicyUsesValidatedRole(string role, HttpStatusCode expected)
    {
        var (userId, session) = await NewAccountAsync(role);
        await using var factory = Factory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, userId, session, role));
        using var response = await client.GetAsync("/__tests/admin");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.Forbidden)
            await AssertError(response, expected, "Identity.Forbidden");
    }

    [Fact]
    public async Task RefreshCookieAndQueryTokenCannotAuthenticateMe()
    {
        var (userId, session) = await NewAccountAsync();
        await using var factory = Factory();
        using var client = Client(factory);
        var token = Token(factory, userId, session);
        client.DefaultRequestHeaders.Add("Cookie", "__Secure-HM.Refresh=" + token);
        using var response = await client.GetAsync("/api/v1/identity/me?access_token=" + token);
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
    }

    [Fact]
    public async Task ValidBearersFromDifferentAccountsDoNotChangeAnonymousCsrfContext()
    {
        var (firstId, firstSession) = await NewAccountAsync();
        var (secondId, secondSession) = await NewAccountAsync();
        await using var factory = Factory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, firstId, firstSession));
        using var bootstrap = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/auth/csrf");
        bootstrap.Headers.Add("Origin", "https://frontend.example.test");
        using var initialized = await client.SendAsync(bootstrap);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var cookie = Assert.Single(initialized.Headers.GetValues("Set-Cookie")).Split(';')[0];
        using var body = JsonDocument.Parse(await initialized.Content.ReadAsStringAsync());
        var csrf = body.RootElement.GetProperty("data").GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, secondId, secondSession));
        using var mutation = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/auth/__tests/csrf");
        mutation.Headers.Add("Origin", "https://frontend.example.test");
        mutation.Headers.Add("Cookie", cookie);
        mutation.Headers.Add("X-CSRF-TOKEN", csrf);
        using var completed = await client.SendAsync(mutation);
        Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);
    }

    [Fact]
    public async Task ProfileQuotaIsSharedByUserAndDoesNotConsumeOtherUsersQuota()
    {
        var (firstId, firstSession) = await NewAccountAsync();
        var (secondId, secondSession) = await NewAccountAsync();
        await using var factory = Factory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, firstId, firstSession));
        for (var index = 0; index < 120; index++)
        {
            using var allowed = await client.GetAsync("/api/v1/identity/me");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        // Issuing another access token does not reset the same user's quota.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, firstId, firstSession));
        using var rejected = await client.GetAsync("/api/v1/identity/me");
        await AssertError(rejected, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory, secondId, secondSession));
        using var otherUser = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, otherUser.StatusCode);
    }
}
