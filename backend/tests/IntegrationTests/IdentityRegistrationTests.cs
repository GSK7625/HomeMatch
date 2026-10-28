using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Modules.Identity.Presentation.Registration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMatch.IntegrationTests;

public class IdentityRegistrationTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "  Mây xanh phủ đồi sáng 2026!  ";

    private ApiFactory Factory(bool available = true) => new(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = available ? database.ConnectionString
            : "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1;Command Timeout=1"
    });

    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://api.example.test"), HandleCookies = false });

    private static object Request(string email, string role = "Tenant", string? password = null, string name = "  Nguyễn Văn An  ")
        => new { email, password = password ?? Password, fullName = name, role };

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(code, body.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("Tenant")]
    [InlineData("Landlord")]
    public async Task RegistrationCommitsCredentialProfileAndRoleWithoutLoggingIn(string role)
    {
        var email = $"New-{Guid.NewGuid():N}+tag@Example.test";
        await using var factory = Factory();
        using var client = Client(factory);
        // Registration is public; no Origin, CSRF, cookie or valid Bearer is needed.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "stale-token");
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request("  " + email + "  ", role));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "message" }, body.RootElement.GetProperty("data").EnumerateObject().Select(property => property.Name));
        Assert.Equal(RegistrationEndpoint.AcceptedMessage, body.RootElement.GetProperty("data").GetProperty("message").GetString());
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = await context.Users.SingleAsync(value => value.NormalizedEmail == email.ToUpperInvariant());
        Assert.Equal(email, account.Email);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.False(account.EmailConfirmed);
        Assert.Equal(1, account.SecurityVersion);
        Assert.True(account.LockoutEnabled);
        Assert.True(await manager.CheckPasswordAsync(account, Password));
        Assert.False(await manager.CheckPasswordAsync(account, Password.Trim()));
        var profile = await context.Profiles.SingleAsync(value => value.UserId == account.Id);
        Assert.Equal("Nguyễn Văn An", profile.FullName);
        Assert.Null(profile.PhoneNumber);
        Assert.Equal(new[] { role }, await manager.GetRolesAsync(account));
        Assert.False(await context.Sessions.AnyAsync(value => value.UserId == account.Id));
    }

    [Fact]
    public async Task DuplicateEmailKeepsCredentialProfileRoleAndStatus()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.test";
        await using var factory = Factory();
        using var client = Client(factory);
        using var first = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var account = await context.Users.SingleAsync(value => value.NormalizedEmail == email.ToUpperInvariant());
            account.Status = AccountStatus.Suspended;
            account.AccessFailedCount = 4;
            account.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            await context.SaveChangesAsync();
        }
        using var duplicate = await client.PostAsJsonAsync("/api/v1/identity/auth/register",
            Request(email.ToUpperInvariant(), "Landlord", "Mật khẩu khác dành cho test 2026!", "Người khác"));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var secondJson = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.Equal(firstJson.RootElement.GetProperty("data").GetRawText(), secondJson.RootElement.GetProperty("data").GetRawText());
        await using var verification = database.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = verification.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var stored = await db.Users.SingleAsync(value => value.NormalizedEmail == email.ToUpperInvariant());
        Assert.Equal(AccountStatus.Suspended, stored.Status);
        Assert.Equal(4, stored.AccessFailedCount);
        Assert.NotNull(stored.LockoutEnd);
        Assert.True(await manager.CheckPasswordAsync(stored, Password));
        Assert.Equal("Nguyễn Văn An", (await db.Profiles.SingleAsync(value => value.UserId == stored.Id)).FullName);
        Assert.Equal(new[] { "Tenant" }, await manager.GetRolesAsync(stored));
        Assert.False(await db.Sessions.AnyAsync(value => value.UserId == stored.Id));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"Mật khẩu hợp lệ 2026!\",\"fullName\":\"An Bình\",\"role\":\"Tenant\",\"status\":\"Active\"}")]
    [InlineData("{\"email\":\"a@example.test\",\"emailVerified\":true,\"password\":\"Mật khẩu hợp lệ 2026!\",\"fullName\":\"An Bình\",\"role\":\"Tenant\"}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"Mật khẩu hợp lệ 2026!\",\"fullName\":\"An Bình\",\"role\":\"Tenant\",\"role\":\"Landlord\"}")]
    [InlineData("{\"Email\":\"a@example.test\",\"password\":\"Mật khẩu hợp lệ 2026!\",\"fullName\":\"An Bình\",\"role\":\"Tenant\"}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":123,\"fullName\":\"An Bình\",\"role\":\"Tenant\"}")]
    [InlineData("{\"email\":null,\"password\":\"Mật khẩu hợp lệ 2026!\",\"fullName\":\"An Bình\",\"role\":\"Tenant\"}")]
    [InlineData("{\"email\":\"a@example.test\",\"password\":\"Mật khẩu hợp lệ 2026!\",\"fullName\":\"\\uD800\",\"role\":\"Tenant\"}")]
    public async Task StrictJsonRejectsInvalidOrUnexpectedFields(string json)
    {
        await using var factory = Factory(available: false);
        using var client = Client(factory);
        using var response = await client.PostAsync("/api/v1/identity/auth/register", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertError(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
    }

    [Theory]
    [InlineData("Admin", "a@example.test", "Mật khẩu hợp lệ 2026!", "An Bình")]
    [InlineData("tenant", "a@example.test", "Mật khẩu hợp lệ 2026!", "An Bình")]
    [InlineData("Tenant", "invalid", "Mật khẩu hợp lệ 2026!", "An Bình")]
    [InlineData("Tenant", "â@example.test", "Mật khẩu hợp lệ 2026!", "An Bình")]
    [InlineData("Tenant", "a@example.test", "short", "An Bình")]
    [InlineData("Tenant", "a@example.test", "123456789012345", "An Bình")]
    [InlineData("Tenant", "a@example.test", "Mật khẩu hợp lệ 2026!", "A")]
    [InlineData("Tenant", "a@example.test", "Mật khẩu hợp lệ 2026!", "An\nBình")]
    public async Task InvalidBusinessInputFailsBeforeDatabase(string role, string email, string password, string name)
    {
        await using var factory = Factory(available: false);
        using var client = Client(factory);
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email, role, password, name));
        await AssertError(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
    }

    [Fact]
    public async Task UnavailableDatabaseDoesNotReportRegistrationAccepted()
    {
        await using var factory = Factory(available: false);
        using var client = Client(factory);
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request("unavailable@example.test"));
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
        Assert.DoesNotContain("Npgsql", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OversizedBodyIsRejectedWithAndWithoutContentLength(bool knownLength)
    {
        await using var factory = Factory(available: false);
        using var client = Client(factory);
        var bytes = Encoding.UTF8.GetBytes(new string(' ', 16385));
        HttpContent content = knownLength ? new ByteArrayContent(bytes) : new StreamingContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await client.PostAsync("/api/v1/identity/auth/register", content);
        await AssertError(response, HttpStatusCode.RequestEntityTooLarge, "Identity.RequestTooLarge");
    }

    [Fact]
    public async Task NonJsonContentIsRejected()
    {
        await using var factory = Factory(available: false);
        using var client = Client(factory);
        using var response = await client.PostAsync("/api/v1/identity/auth/register", new StringContent("email=a@example.test"));
        await AssertError(response, HttpStatusCode.UnsupportedMediaType, "Identity.UnsupportedMediaType");
    }

    [Fact]
    public async Task EmailQuotaCountsNonexistentEmailsAndNormalizedVariants()
    {
        await using var factory = Factory(available: false);
        using var client = Client(factory);
        for (var index = 0; index < 3; index++)
        {
            using var attempt = await client.PostAsJsonAsync("/api/v1/identity/auth/register",
                Request(index % 2 == 0 ? "quota@example.test" : " QUOTA@EXAMPLE.TEST ", "Admin"));
            Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
        }
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request("quota@example.test"));
        await AssertError(response, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task IpQuotaCountsMalformedRequestsAndDoesNotTrustForwardedFor()
    {
        await using var factory = Factory(available: false);
        using var client = Client(factory);
        for (var index = 0; index < 5; index++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/auth/register")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Forwarded-For", "192.0.2." + index);
            using var attempt = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
        }
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request("new@example.test"));
        await AssertError(response, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task FailureAfterCredentialCreationRollsBackCredentialProfileAndRole()
    {
        var email = $"rollback-http-{Guid.NewGuid():N}@example.test";
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION identity.t06_fail_profile() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'private-test-rollback-marker'; END; $$;
            CREATE TRIGGER t06_fail_profile BEFORE INSERT ON identity."UserProfiles"
            FOR EACH ROW EXECUTE FUNCTION identity.t06_fail_profile();
            """);
        try
        {
            await using var factory = Factory();
            using var client = Client(factory);
            using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email));
            await AssertError(response, HttpStatusCode.InternalServerError, "Server.InternalError");
            Assert.DoesNotContain("private-test-rollback-marker", await response.Content.ReadAsStringAsync());
            Assert.False(await db.Users.AnyAsync(value => value.NormalizedEmail == email.ToUpperInvariant()));
            Assert.False(await db.Profiles.AnyAsync(profile => !db.Users.Any(user => user.Id == profile.UserId)));
            Assert.False(await db.UserRoles.AnyAsync(role => !db.Users.Any(user => user.Id == role.UserId)));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER t06_fail_profile ON identity."UserProfiles";
                DROP FUNCTION identity.t06_fail_profile();
                """);
        }
    }

    [Fact]
    public async Task ConcurrentRegistrationUsesDatabaseGuardAndHashesExistingEmailToo()
    {
        var email = $"race-http-{Guid.NewGuid():N}@example.test";
        var barrier = new InsertBarrier();
        var hasher = new CountingHasher();
        await using var original = Factory();
        await using var factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier));
            services.RemoveAll<IPasswordHasher<IdentityAccount>>();
            services.AddSingleton<IPasswordHasher<IdentityAccount>>(hasher);
        }));
        using var client = Client(factory);
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email)),
            client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email.ToUpperInvariant(), "Landlord")));
        foreach (var attempt in attempts) { using (attempt) Assert.Equal(HttpStatusCode.OK, attempt.StatusCode); }
        Assert.Equal(2, barrier.Arrivals);
        Assert.Equal(2, hasher.Hashes);
        using var duplicate = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(3, hasher.Hashes);
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var account = await context.Users.SingleAsync(value => value.NormalizedEmail == email.ToUpperInvariant());
        Assert.Equal(1, await context.Profiles.CountAsync(value => value.UserId == account.Id));
        Assert.Equal(1, await context.UserRoles.CountAsync(value => value.UserId == account.Id));
    }

    [Fact]
    public async Task IdentityChangePasswordUsesTheSamePolicyWithoutApplyingItToLoginVerification()
    {
        var email = $"change-policy-{Guid.NewGuid():N}@example.test";
        await using var factory = Factory();
        using var client = Client(factory);
        using var response = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = database.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = (await manager.FindByEmailAsync(email))!;
        var hash = account.PasswordHash;
        var changed = await manager.ChangePasswordAsync(account, Password, "123456789012345");
        Assert.False(changed.Succeeded);
        Assert.Equal(hash, account.PasswordHash);
        Assert.True(await manager.CheckPasswordAsync(account, Password));
        var invalid = new IdentityAccount("direct-" + Guid.NewGuid().ToString("N") + "@example.test", DateTimeOffset.UtcNow);
        Assert.False((await manager.CreateAsync(invalid, "123456789012345")).Succeeded);
    }

    [Fact]
    public async Task UnicodeScalarBoundariesAreEnforcedThroughHttpAndIdentity()
    {
        var email = $"unicode-{Guid.NewGuid():N}@example.test";
        var password = string.Concat(Enumerable.Repeat("😀", 128));
        var name = string.Concat(Enumerable.Repeat("😀", 100));
        await using var factory = Factory();
        using var client = Client(factory);
        using var accepted = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email, password: password, name: name));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var invalid = await client.PostAsJsonAsync("/api/v1/identity/auth/register", Request(email + ".other", password: password + "😀"));
        await AssertError(invalid, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
        await using var scope = database.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = (await manager.FindByEmailAsync(email))!;
        Assert.True(await manager.CheckPasswordAsync(account, password));
        var direct = new IdentityAccount("direct-unicode-" + Guid.NewGuid().ToString("N") + "@example.test", DateTimeOffset.UtcNow);
        Assert.False((await manager.CreateAsync(direct, string.Concat(Enumerable.Repeat("😀", 14)))).Succeeded);
    }

    private sealed class StreamingContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(bytes).AsTask();
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }

    private sealed class CountingHasher : IPasswordHasher<IdentityAccount>
    {
        private readonly PasswordHasher<IdentityAccount> _inner = new();
        private int _hashes;
        public int Hashes => _hashes;
        public string HashPassword(IdentityAccount user, string password)
        { Interlocked.Increment(ref _hashes); return _inner.HashPassword(user, password); }
        public PasswordVerificationResult VerifyHashedPassword(IdentityAccount user, string hash, string password)
            => _inner.VerifyHashedPassword(user, hash, password);
    }

    private sealed class InsertBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => _arrivals;
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO identity.\"Users\"", StringComparison.Ordinal))
            {
                var number = Interlocked.Increment(ref _arrivals);
                if (number <= 2)
                {
                    if (number == 2) _ready.TrySetResult();
                    await _ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
                }
            }
            return result;
        }
    }
}
