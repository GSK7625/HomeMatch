using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMatch.IntegrationTests;

public partial class IdentityEmailVerificationTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Root = "/api/v1/identity";
    private const string Password = "Mật khẩu kiểm thử email OTP 2026!";
    private readonly string _jwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string _otpKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private WebApplicationFactory<Program> Factory(Mailbox mailbox, Clock clock, bool enabled = true,
        Action<IServiceCollection>? extra = null, bool availableDatabase = true)
    {
        var original = new ApiFactory(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = _jwt,
            ["ConnectionStrings:DefaultConnection"] = availableDatabase ? database.ConnectionString
                : "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1;Command Timeout=1",
            ["Email:Enabled"] = enabled.ToString(),
            ["Email:Smtp:Username"] = "sender@gmail.com", ["Email:Smtp:FromAddress"] = "sender@gmail.com",
            ["Email:Smtp:AppPassword"] = "abcdefghijklmnop",
            ["Identity:EmailVerification:Keys:v1"] = _otpKey
        });
        return original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITransactionalEmailSender>(); services.AddSingleton<ITransactionalEmailSender>(mailbox);
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock);
            extra?.Invoke(services);
        }));
    }

    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://api.example.test"), HandleCookies = false, AllowAutoRedirect = false });

    private static async Task<User> RegisterLogin(HttpClient client, string role = "Tenant")
    {
        var email = $"otp-{Guid.NewGuid():N}@example.test";
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using (var csrf = await client.GetAsync(Root + "/auth/csrf"))
        {
            Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
            client.DefaultRequestHeaders.Add("Cookie", Assert.Single(csrf.Headers.GetValues("Set-Cookie")).Split(';')[0]);
            var data = await Data(csrf);
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", data.GetProperty("requestToken").GetString());
        }
        using (var register = await client.PostAsJsonAsync(Root + "/auth/register",
                   new { email, password = Password, fullName = "Nguyễn Văn An", role }))
            Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        using var login = await client.PostAsJsonAsync(Root + "/auth/login", new { email, password = Password, rememberMe = true });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await Data(login)).GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var me = await client.GetAsync(Root + "/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await Data(me);
        Assert.False(profile.GetProperty("emailVerified").GetBoolean());
        return new(profile.GetProperty("id").GetGuid(), token, email);
    }

    private static Task<HttpResponseMessage> Request(HttpClient client) => client.PostAsync(Root + "/auth/resend-verification", null);
    private static Task<HttpResponseMessage> Verify(HttpClient client, Guid id, string code)
        => client.PostAsJsonAsync(Root + "/auth/verify-email", new { challengeId = id, code });
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("isSuccess").GetBoolean());
        return json.RootElement.GetProperty("data").Clone();
    }
    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("error").GetProperty("code").GetString());
    }
    private async Task<EmailVerificationChallenge> Challenge(Guid id)
    {
        await using var scope = database.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().EmailVerificationChallenges.AsNoTracking().SingleAsync(value => value.Id == id);
    }
    private static string Code(TransactionalEmail message) => Regex.Match(message.TextBody, "\\b[0-9]{6}\\b").Value;
    private static string Wrong(string code) => code == "000000" ? "000001" : "000000";

    [Theory]
    [InlineData("Tenant")]
    [InlineData("Landlord")]
    public async Task RegisterLoginSendVerifyAndMeWorkWithoutRotatingOrRevokingTheSession(string role)
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock);
        using var client = Client(factory);
        var user = await RegisterLogin(client, role);
        Assert.Empty(mailbox.Messages);
        client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Remove("Origin"); // These operations authenticate only by Bearer.
        using var request = await Request(client);
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);
        var data = await Data(request);
        Assert.Equal("Submitted", data.GetProperty("status").GetString());
        var id = data.GetProperty("challengeId").GetGuid();
        var message = Assert.Single(mailbox.Messages);
        var code = Code(message);
        Assert.Matches("^[0-9]{6}$", code); Assert.Equal(user.Email, message.Recipient);
        Assert.DoesNotContain(code, await request.Content.ReadAsStringAsync());
        Assert.False(request.Headers.Contains("Set-Cookie"));
        var stored = await Challenge(id);
        Assert.Equal(EmailVerificationState.Sent, stored.State); Assert.Equal(0, stored.FailedAttempts);
        Assert.Equal(TimeSpan.FromMinutes(5), stored.ExpiresAt - stored.CreatedAt);
        Assert.NotEqual(code, stored.CodeMac);
        using var verified = await Verify(client, id, code);
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode); Assert.True((await Data(verified)).GetProperty("emailVerified").GetBoolean());
        Assert.False(verified.Headers.Contains("Set-Cookie"));
        using var me = await client.GetAsync(Root + "/me");
        Assert.True((await Data(me)).GetProperty("emailVerified").GetBoolean());
        using var replay = await Verify(client, id, code);
        await AssertError(replay, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        using var already = await Request(client);
        Assert.Equal("AlreadyVerified", (await Data(already)).GetProperty("status").GetString());
        Assert.Single(mailbox.Messages);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var account = await db.Users.SingleAsync(value => value.Id == user.Id);
        Assert.Equal(1L, account.SecurityVersion); Assert.Equal(0, account.AccessFailedCount);
        Assert.Null((await db.Sessions.SingleAsync(value => value.UserId == user.Id)).RevokedAt);
        Assert.Equal(EmailVerificationState.Consumed, (await Challenge(id)).State);
    }

    [Fact]
    public async Task FifthWrongCodeLocksChallengeWithoutPasswordLockoutOrSessionRevocation()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        using var sent = await Request(client); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        var code = Code(Assert.Single(mailbox.Messages));
        for (var index = 0; index < 5; index++)
        {
            using var wrong = await Verify(client, id, Wrong(code));
            await AssertError(wrong, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        }
        using var locked = await Verify(client, id, code);
        await AssertError(locked, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        var challenge = await Challenge(id);
        Assert.Equal(5, challenge.FailedAttempts); Assert.Equal(EmailVerificationState.Locked, challenge.State);
        using var me = await client.GetAsync(Root + "/me"); Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        await using var scope = database.Services.CreateAsyncScope();
        var account = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.SingleAsync(value => value.Id == user.Id);
        Assert.False(account.EmailConfirmed); Assert.Equal(0, account.AccessFailedCount); Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public async Task ExpiryAtExactBoundaryAndResendInvalidateOldChallenge()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        await RegisterLogin(client);
        using var first = await Request(client); var oldId = (await Data(first)).GetProperty("challengeId").GetGuid();
        var oldCode = Code(mailbox.Messages.Last());
        clock.Advance(TimeSpan.FromMinutes(5));
        using var expired = await Verify(client, oldId, oldCode);
        await AssertError(expired, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        using var resend = await Request(client); Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
        var newId = (await Data(resend)).GetProperty("challengeId").GetGuid();
        Assert.NotEqual(oldId, newId); Assert.Equal(EmailVerificationState.Invalidated, (await Challenge(oldId)).State);
        using var old = await Verify(client, oldId, oldCode);
        await AssertError(old, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        using var correct = await Verify(client, newId, Code(mailbox.Messages.Last()));
        Assert.Equal(HttpStatusCode.OK, correct.StatusCode);
    }

    [Theory]
    [InlineData(EmailSubmissionOutcome.Rejected)]
    [InlineData(EmailSubmissionOutcome.Unknown)]
    [InlineData(EmailSubmissionOutcome.Unavailable)]
    public async Task FailedSubmissionIsNeverVerifiableAndStillConsumesCooldown(EmailSubmissionOutcome outcome)
    {
        var mailbox = new Mailbox { Outcome = outcome }; var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        using var failed = await Request(client);
        await AssertError(failed, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var challenge = await db.EmailVerificationChallenges.SingleAsync(value => value.UserId == user.Id);
        Assert.Equal(EmailVerificationState.Failed, challenge.State);
        using var verify = await Verify(client, challenge.Id, Code(mailbox.Messages.Last()));
        await AssertError(verify, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        using var repeated = await Request(client); Assert.Equal(HttpStatusCode.TooManyRequests, repeated.StatusCode);
        Assert.True(repeated.Headers.RetryAfter?.Delta > TimeSpan.Zero); Assert.Single(mailbox.Messages);
    }

    private sealed record User(Guid Id, string Token, string Email);
    private sealed class Clock : TimeProvider
    {
        private long _ticks = DateTimeOffset.UtcNow.UtcTicks;
        public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);
        public void Advance(TimeSpan duration) => Interlocked.Add(ref _ticks, duration.Ticks);
    }
    private sealed class Mailbox : ITransactionalEmailSender
    {
        public bool IsAvailable => true;
        public string SenderKey { get; } = Guid.NewGuid().ToString("N");
        public readonly ConcurrentQueue<TransactionalEmail> Messages = new();
        public EmailSubmissionOutcome Outcome = EmailSubmissionOutcome.Accepted;
        public Func<CancellationToken, Task>? BeforeReturn;
        public async Task<EmailSubmissionOutcome> SendAsync(TransactionalEmail message, CancellationToken cancellationToken)
        {
            Messages.Enqueue(message);
            if (BeforeReturn is not null) await BeforeReturn(cancellationToken);
            return Outcome;
        }
    }
}
