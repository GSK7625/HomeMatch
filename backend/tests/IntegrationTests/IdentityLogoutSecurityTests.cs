using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

public partial class IdentityLogoutTests
{
    [Theory]
    [InlineData("origin")]
    [InlineData("csrf")]
    [InlineData("cookie")]
    public async Task CurrentLogoutRejectsBadOriginOrCsrfWithoutRevocation(string invalid)
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var login = await Login(client, account);
        SetRefresh(client, login.Raw);
        client.DefaultRequestHeaders.Remove(invalid == "origin" ? "Origin" : invalid == "csrf" ? "X-CSRF-TOKEN" : "Cookie");
        using var response = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertError(response, HttpStatusCode.Forbidden, invalid == "origin" ? "Identity.OriginNotAllowed" : "Identity.CsrfInvalid");
        await AssertMe(client, login.Access, HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("logout")]
    [InlineData("logout-all")]
    public async Task LogoutRejectsClientSelectedUserAndMediaTypeWithoutMutations(string route)
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var login = await Login(client, account);
        SetRefresh(client, login.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        using var body = await client.PostAsJsonAsync("/api/v1/identity/auth/" + route, new { userId = Guid.NewGuid() });
        await AssertError(body, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
        using var media = await client.PostAsync("/api/v1/identity/auth/" + route, new StringContent("{}"));
        await AssertError(media, HttpStatusCode.UnsupportedMediaType, "Identity.UnsupportedMediaType");
        using var large = await client.PostAsync("/api/v1/identity/auth/" + route,
            new StringContent(new string(' ', 16385), Encoding.UTF8, "application/json"));
        await AssertError(large, HttpStatusCode.RequestEntityTooLarge, "Identity.RequestTooLarge");
        await AssertMe(client, login.Access, HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("{\"refreshToken\":\"not-accepted\"}")]
    public async Task LogoutRequiresEmptyObjectOrOmittedBody(string json)
    {
        await using var factory = Factory(available: false);
        using var client = await Browser(factory);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertError(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
    }

    [Fact]
    public async Task LogoutAllRequiresBearerAndDoesNotAcceptRefreshCookieAsIdentity()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var login = await Login(client, account);
        SetRefresh(client, login.Raw);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
        await AssertMe(client, login.Access, HttpStatusCode.OK);
    }

    [Fact]
    public async Task CurrentLogoutUnavailableDatabaseDoesNotDeleteCookie()
    {
        await using var factory = Factory(available: false);
        using var client = await Browser(factory);
        SetRefresh(client, RefreshSecrets.Create().Raw);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
    }

    [Fact]
    public async Task LogoutAllAuthenticationStoreFailureDoesNotDeleteCookie()
    {
        var account = await Account();
        await using var original = Factory();
        using var initialClient = await Browser(original);
        var login = await Login(initialClient, account);
        await using var unavailable = Factory(available: false);
        using var client = await Browser(unavailable);
        SetRefresh(client, login.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
    }

    [Fact]
    public async Task MissingCookieCanBeLoggedOutWithoutDatabase()
    {
        await using var factory = Factory(available: false);
        using var client = await Browser(factory);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertNoContent(response, true);
    }

    [Fact]
    public async Task CurrentLogoutQuotaAppliesToIdempotentRequestsAndDoesNotTrustForwardedFor()
    {
        await using var factory = Factory();
        using var client = await Browser(factory);
        for (var index = 0; index < 30; index++)
        {
            client.DefaultRequestHeaders.Remove("X-Forwarded-For");
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "192.0.2." + index);
            using var response = await client.PostAsync("/api/v1/identity/auth/logout", null);
            await AssertNoContent(response, true);
        }
        using var rejected = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertError(rejected, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task LogoutAllUserQuotaCountsValidatedRequestsEvenWhenBodyIsInvalid()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var login = await Login(client, account);
        SetRefresh(client, login.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        for (var index = 0; index < 5; index++)
        {
            using var invalid = await client.PostAsJsonAsync("/api/v1/identity/auth/logout-all", new { userId = account.Id });
            await AssertError(invalid, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
        }
        using var rejected = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertError(rejected, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.FromMinutes(1));
        await AssertMe(client, login.Access, HttpStatusCode.OK);
    }

    [Fact]
    public async Task DevHttpLogoutDeletesOnlyDevelopmentRefreshCookie()
    {
        var account = await Account();
        await using var factory = Factory(environment: "Development");
        using var client = await Browser(factory, "http://localhost");
        var login = await Login(client, account);
        SetRefresh(client, login.Raw, "HM.Refresh.Dev");
        using var response = await client.PostAsync("/api/v1/identity/auth/logout", null);
        await AssertNoContent(response, true);
        var header = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.StartsWith("hm.refresh.dev=", header);
        Assert.DoesNotContain("; secure", header);
    }

    [Fact]
    public async Task LogoutAllClearsOwnedCookieEvenWhenItIsFromConsumedHistory()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        var login = await Login(client, account);
        SetRefresh(client, login.Raw);
        using var rotated = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        // The old JWT remains usable after ordinary rotation, and the old cookie still proves ownership.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertNoContent(response, true);
    }
}
