using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

public partial class IdentityLoginTests
{
    [Theory]
    [InlineData("origin")]
    [InlineData("csrf")]
    [InlineData("cookie")]
    public async Task LoginRejectsOriginOrCsrfBeforeCredentialSideEffects(string invalid)
    {
        var account = await AccountAsync();
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        client.DefaultRequestHeaders.Remove(invalid == "origin" ? "Origin" : invalid == "csrf" ? "X-CSRF-TOKEN" : "Cookie");
        using var response = await Login(client, account.Email!, "wrong");
        await AssertError(response, HttpStatusCode.Forbidden, invalid == "origin" ? "Identity.OriginNotAllowed" : "Identity.CsrfInvalid");
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(0, (await db.Users.SingleAsync(user => user.Id == account.Id)).AccessFailedCount);
        Assert.False(await db.Sessions.AnyAsync(value => value.UserId == account.Id));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"old\",\"rememberMe\":\"true\"}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"old\"}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"old\",\"rememberMe\":null}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"old\",\"rememberMe\":false,\"role\":\"Admin\"}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"old\",\"rememberMe\":false,\"rememberMe\":true}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"\\uD800\",\"rememberMe\":false}")]
    public async Task LoginStrictJsonIsValidatedBeforeDatabase(string json)
    {
        await using var factory = Factory(available: false);
        using var client = await BrowserAsync(factory);
        using var response = await client.PostAsync("/api/v1/identity/auth/login", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertError(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
    }

    [Theory]
    [InlineData("email")]
    [InlineData("length")]
    [InlineData("empty")]
    public async Task InvalidLoginBusinessInputDoesNotReachDatabase(string invalid)
    {
        await using var factory = Factory(available: false);
        using var client = await BrowserAsync(factory);
        using var response = await Login(client, invalid == "email" ? "invalid" : "a@example.test",
            invalid == "length" ? new string('a', 129) : invalid == "empty" ? "" : Password);
        await AssertError(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
    }

    [Fact]
    public async Task LoginUnavailableDatabaseReturns503WithoutRefreshCookie()
    {
        await using var factory = Factory(available: false);
        using var client = await BrowserAsync(factory);
        using var response = await Login(client, "a@example.test");
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
    }

    [Fact]
    public async Task LoginMediaTypeAndSizeErrorsHavePublicEnvelope()
    {
        await using var factory = Factory(available: false);
        using var client = await BrowserAsync(factory);
        using var media = await client.PostAsync("/api/v1/identity/auth/login", new StringContent("not-json"));
        await AssertError(media, HttpStatusCode.UnsupportedMediaType, "Identity.UnsupportedMediaType");
        using var large = await client.PostAsync("/api/v1/identity/auth/login",
            new StringContent(new string(' ', 16385), Encoding.UTF8, "application/json"));
        await AssertError(large, HttpStatusCode.RequestEntityTooLarge, "Identity.RequestTooLarge");
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("missing")]
    [InlineData("locked")]
    public async Task CredentialFailuresAreIndistinguishableInResponse(string kind)
    {
        var account = await AccountAsync();
        if (kind == "locked")
        {
            await using var scope = database.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var stored = await db.Users.SingleAsync(value => value.Id == account.Id);
            stored.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            await db.SaveChangesAsync();
        }
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var response = await Login(client, kind == "missing" ? Guid.NewGuid().ToString("N") + "@example.test" : account.Email!,
            kind == "wrong" ? "incorrect" : Password);
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        await using var verification = database.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.False(await context.Sessions.AnyAsync(value => value.UserId == account.Id));
    }

    [Fact]
    public async Task SuspendedStatusIsDisclosedOnlyAfterCorrectPassword()
    {
        var account = await AccountAsync();
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await db.Users.SingleAsync(value => value.Id == account.Id)).Status = AccountStatus.Suspended;
            await db.SaveChangesAsync();
        }
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        using var wrong = await Login(client, account.Email!, "incorrect");
        await AssertError(wrong, HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        using var correct = await Login(client, account.Email!);
        await AssertError(correct, HttpStatusCode.Forbidden, "Identity.AccountSuspended");
    }

    [Fact]
    public async Task LoginEmailQuotaCountsNormalizedNonexistentEmail()
    {
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        for (var index = 0; index < 5; index++)
        {
            using var attempt = await Login(client, index % 2 == 0 ? email : " " + email.ToUpperInvariant() + " ");
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }
        using var rejected = await Login(client, email);
        await AssertError(rejected, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task LoginIpQuotaCannotBeBypassedWithForwardedFor()
    {
        await using var factory = Factory();
        using var client = await BrowserAsync(factory);
        for (var index = 0; index < 10; index++)
        {
            client.DefaultRequestHeaders.Remove("X-Forwarded-For");
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "192.0.2." + index);
            using var response = await Login(client, "invalid");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var rejected = await Login(client, "invalid");
        await AssertError(rejected, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
    }
}
