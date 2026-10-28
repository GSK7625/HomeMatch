using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using HomeMatch.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HomeMatch.IntegrationTests;

public class IdentityCsrfTests
{
    private static HttpClient Client(ApiFactory factory, string url = "https://api.example.test")
        => factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(url), HandleCookies = false, AllowAutoRedirect = false
        });

    private static async Task<(string Token, string Cookie)> Bootstrap(HttpClient client, string? cookie = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/auth/csrf");
        request.Headers.Add("Origin", "https://frontend.example.test");
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var headers) ? headers.FirstOrDefault() : null;
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (body.RootElement.GetProperty("data").GetProperty("requestToken").GetString()!,
            setCookie?.Split(';')[0] ?? cookie!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("https://attacker.example.test")]
    [InlineData("https://frontend.example.test.attacker.test")]
    [InlineData("https://frontend.example.test/")]
    public async Task BootstrapRejectsMissingOrUntrustedOrigin(string? origin)
    {
        await using var factory = new ApiFactory();
        using var client = Client(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/auth/csrf");
        if (origin is not null) request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Contains("Identity.OriginNotAllowed", await response.Content.ReadAsStringAsync());
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task SameOriginFetchMetadataRequiresConfiguredTargetOrigin()
    {
        await using var factory = new ApiFactory();
        using var trusted = Client(factory, "https://frontend.example.test");
        trusted.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        using var accepted = await trusted.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var other = Client(factory);
        other.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        using var rejected = await other.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
    }

    [Fact]
    public async Task ProductionBootstrapUsesSecureHostOnlyCookieAndIgnoresBearer()
    {
        await using var factory = new ApiFactory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "stale-token-with-no-valid-signature");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/auth/csrf");
        request.Headers.Add("Origin", "https://frontend.example.test");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.StartsWith("__host-hm.antiforgery=", cookie);
        Assert.Contains("path=/", cookie);
        Assert.Contains("; secure", cookie);
        Assert.Contains("; httponly", cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.DoesNotContain("domain=", cookie);
        Assert.DoesNotContain("expires=", cookie);
    }

    [Fact]
    public async Task DevelopmentHttpUsesDevelopmentCookieOnlyOnLoopback()
    {
        await using var factory = new ApiFactory(environment: "Development");
        using var client = Client(factory, "http://localhost");
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using var accepted = await client.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var cookie = Assert.Single(accepted.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.StartsWith("hm.antiforgery.dev=", cookie);
        Assert.DoesNotContain("; secure", cookie);
        using var remote = Client(factory, "http://remote.example.test");
        remote.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using var rejected = await remote.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
    }

    [Fact]
    public async Task ProductionNeverDowngradesCookieSecurityForHttp()
    {
        await using var factory = new ApiFactory();
        using var client = Client(factory, "http://localhost");
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using var response = await client.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("none")]
    [InlineData("cookie")]
    [InlineData("token")]
    [InlineData("mismatched")]
    [InlineData("origin")]
    public async Task MutationRequiresMatchingCookieTokenAndOrigin(string invalid)
    {
        await using var factory = new ApiFactory();
        using var client = Client(factory);
        var pair = await Bootstrap(client);
        var otherPair = await Bootstrap(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/auth/__tests/csrf");
        request.Headers.Add("Origin", invalid == "origin" ? "https://attacker.example.test" : "https://frontend.example.test");
        if (invalid is not ("none" or "cookie")) request.Headers.Add("Cookie", pair.Cookie);
        if (invalid is not ("none" or "token"))
            request.Headers.Add("X-CSRF-TOKEN", invalid == "mismatched" ? otherPair.Token : pair.Token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(invalid == "origin" ? "Identity.OriginNotAllowed" : "Identity.CsrfInvalid",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TwoTabsCanReuseCookieAndAnonymousTokenDespiteBearerChanges()
    {
        await using var factory = new ApiFactory();
        using var client = Client(factory);
        var first = await Bootstrap(client);
        var second = await Bootstrap(client, first.Cookie);
        Assert.Equal(first.Cookie, second.Cookie);
        foreach (var token in new[] { first.Token, second.Token })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/auth/__tests/csrf");
            request.Headers.Add("Origin", "https://frontend.example.test");
            request.Headers.Add("Cookie", first.Cookie);
            request.Headers.Add("X-CSRF-TOKEN", token);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "old-account-or-expired-token");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task BootstrapQuotaReturns429AndRetryAfter()
    {
        await using var factory = new ApiFactory();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        for (var index = 0; index < 60; index++)
        {
            using var allowed = await client.GetAsync("/api/v1/identity/auth/csrf");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        using var rejected = await client.GetAsync("/api/v1/identity/auth/csrf");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        Assert.Contains("Identity.RateLimitExceeded", await rejected.Content.ReadAsStringAsync());
        Assert.Contains("no-store", rejected.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task UnimplementedPasswordRecoveryRoutesRemainAbsent()
    {
        await using var factory = new ApiFactory();
        using var client = Client(factory);
        foreach (var route in new[] { "forgot-password", "reset-password" })
        {
            using var response = await client.PostAsync("/api/v1/identity/auth/" + route, null);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
