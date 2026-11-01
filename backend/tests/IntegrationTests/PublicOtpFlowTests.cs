using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace HomeMatch.IntegrationTests;
public sealed class PublicOtpFlowTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Root = "/api/v1/identity/auth/";
    private sealed class Mailbox : ITransactionalEmailSender
    {
        public bool IsAvailable => true;
        public string SenderKey { get; } = Guid.NewGuid().ToString("N");
        public ConcurrentQueue<TransactionalEmail> Messages { get; } = new();
        public Task<EmailSubmissionOutcome> SendAsync(TransactionalEmail m, CancellationToken ct)
        { Messages.Enqueue(m); return Task.FromResult(EmailSubmissionOutcome.Accepted); }
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    { response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data"); }

    [Theory]
    [InlineData("registration/send-otp")]
    [InlineData("forgot-password")]
    [InlineData("registration/verify-otp")]
    [InlineData("forgot-password/verify-otp")]
    [InlineData("reset-password")]
    public async Task UnavailableDatabaseReturnsDependencyFailureWithoutExposingProviderDetails(string route)
    {
        await using var factory = new ApiFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1;Pooling=false"
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://api.example.test") });
        object input = route.Contains("verify-otp")
            ? new { challengeId = Guid.NewGuid(), code = "123456" }
            : route == "reset-password"
                ? new { resetToken = HomeMatch.Modules.Identity.Infrastructure.Authentication.RefreshSecrets.Create().Raw, newPassword = "Password123" }
                : route == "registration/send-otp"
                    ? new { email = "test@example.test", password = "Password123" }
                    : new { email = "test@example.test" };
        using var response = await client.PostAsJsonAsync(Root + route, input);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("Server.DependencyUnavailable", body.GetProperty("error").GetProperty("code").GetString());
        Assert.DoesNotContain("Npgsql", body.GetRawText());
    }

    [Theory]
    [InlineData("normal")]
    [InlineData("locked")]
    [InlineData("expired")]
    public async Task RegistrationOtpThenTwoStepRecoveryRevokesSessionAndRejectsReplay(string mode)
    {
        var mail = new Mailbox();
        await using var original = new ApiFactory(new Dictionary<string,string?> {
            ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
            ["Email:Enabled"] = "true", ["Email:Smtp:Username"] = "sender@gmail.com",
            ["Email:Smtp:FromAddress"] = "sender@gmail.com", ["Email:Smtp:AppPassword"] = "abcdefghijklmnop",
            ["Identity:EmailVerification:Keys:v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) });
        await using var factory = original.WithWebHostBuilder(b => b.ConfigureTestServices(s => {
            s.RemoveAll<ITransactionalEmailSender>(); s.AddSingleton<ITransactionalEmailSender>(mail); }));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://api.example.test"), HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using var csrf = await client.GetAsync(Root + "csrf");
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(csrf.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await Data(csrf)).GetProperty("requestToken").GetString());
        var email = $"public-{Guid.NewGuid():N}@example.test"; const string password = "Password123";
        using var registered = await client.PostAsJsonAsync(Root + "register", new { email, password, fullName = "Test User", role = "Tenant" });
        var id = (await Data(registered)).GetProperty("challengeId").GetGuid();
        var code = Regex.Match(Assert.Single(mail.Messages).TextBody, "[0-9]{6}").Value;
        using var blocked = await client.PostAsJsonAsync(Root+"login", new { email, password });
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        if (mode != "normal")
        {
            if (mode == "locked")
                for (var i = 0; i < 5; i++)
                {
                    using var wrong = await client.PostAsJsonAsync(Root+"registration/verify-otp", new { challengeId = id, code = code == "000000" ? "000001" : "000000" });
                    Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
                }
            await using (var edit = database.Services.CreateAsyncScope())
            {
                var store = edit.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var challenge = await store.PublicOtpChallenges.SingleAsync(x => x.Id == id);
                challenge.CreatedAt = challenge.CreatedAt.AddMinutes(-6);
                challenge.ExpiresAt = challenge.ExpiresAt.AddMinutes(-6);
                await store.SaveChangesAsync();
            }
            using var invalid = await client.PostAsJsonAsync(Root+"registration/verify-otp", new { challengeId = id, code });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var sentAgain = await client.PostAsJsonAsync(Root+"registration/send-otp", new { email, password });
            id = (await Data(sentAgain)).GetProperty("challengeId").GetGuid();
            code = Regex.Match(mail.Messages.Last().TextBody, "[0-9]{6}").Value;
        }
        using var verified = await client.PostAsJsonAsync(Root+"registration/verify-otp", new { challengeId = id, code });
        Assert.Equal("Verified", (await Data(verified)).GetProperty("status").GetString());
        using var replay = await client.PostAsJsonAsync(Root+"registration/verify-otp", new { challengeId = id, code });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        using var login = await client.PostAsJsonAsync(Root+"login", new { email, password }); await Data(login);
        using var forgot = await client.PostAsJsonAsync(Root+"forgot-password", new { email });
        var resetId = (await Data(forgot)).GetProperty("challengeId").GetGuid();
        var resetCode = Regex.Match(mail.Messages.Last().TextBody, "[0-9]{6}").Value;
        using var crossPurpose = await client.PostAsJsonAsync(Root+"registration/verify-otp", new { challengeId = resetId, code = resetCode });
        Assert.Equal(HttpStatusCode.BadRequest, crossPurpose.StatusCode);
        using var otp = await client.PostAsJsonAsync(Root+"forgot-password/verify-otp", new { challengeId = resetId, code = resetCode });
        var resetToken = (await Data(otp)).GetProperty("resetToken").GetString();
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync(Root+"reset-password", new { resetToken, newPassword = "Changed123" }),
            client.PostAsJsonAsync(Root+"reset-password", new { resetToken, newPassword = "Changed123" }));
        Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.BadRequest);
        foreach (var attempt in attempts) attempt.Dispose();
        using var used = await client.PostAsJsonAsync(Root+"reset-password", new { resetToken, newPassword = "Changed456" });
        Assert.Equal(HttpStatusCode.BadRequest, used.StatusCode);
        using var oldLogin = await client.PostAsJsonAsync(Root+"login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        using var newLogin = await client.PostAsJsonAsync(Root+"login", new { email, password = "Changed123" }); await Data(newLogin);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var account = await db.Users.SingleAsync(x => x.Email == email);
        Assert.True(account.EmailConfirmed); Assert.Equal(2, account.SecurityVersion);
        Assert.Equal(1, await db.Sessions.CountAsync(x => x.UserId == account.Id && x.RevokedAt != null));
    }
}
