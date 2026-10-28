using System.Net;
using System.Net.Http.Headers;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HomeMatch.IntegrationTests;

public partial class IdentityEmailVerificationTests
{
    [Fact]
    public async Task SenderBudgetIsAtomicAcrossDifferentAccountsAndRenewsAnExpiredUtcWindow()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock);
        using var first = Client(factory); using var second = Client(factory);
        await RegisterLogin(first); await RegisterLogin(second);
        using (var initial = await Request(first)) Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        clock.Advance(TimeSpan.FromSeconds(60));
        await using (var scope = database.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().EmailVerificationQuotas
                .Where(value => value.Key == "sender:" + mailbox.SenderKey).ExecuteUpdateAsync(update => update.SetProperty(value => value.Count, 99));
        var results = await Task.WhenAll(Request(first), Request(second));
        using var one = results[0]; using var two = results[1];
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.TooManyRequests }, results.Select(value => value.StatusCode).OrderBy(value => (int)value));
        Assert.Equal(2, mailbox.Messages.Count);
        clock.Advance(TimeSpan.FromSeconds(60));
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var end = clock.GetUtcNow().AddSeconds(-1); var start = end.AddDays(-1);
            await db.EmailVerificationQuotas.Where(value => value.Key == "sender:" + mailbox.SenderKey)
                .ExecuteUpdateAsync(update => update.SetProperty(value => value.WindowStart, start).SetProperty(value => value.WindowEnd, end));
        }
        using var renewed = await Request(first); Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var quota = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().EmailVerificationQuotas.SingleAsync(value => value.Key == "sender:" + mailbox.SenderKey);
            Assert.Equal(1, quota.Count); Assert.Equal(TimeSpan.Zero, quota.WindowStart.TimeOfDay);
        }
    }

    [Fact]
    public async Task UserDailyQuotaIsEnforcedEvenWhenTheFifteenMinuteWindowHasCapacity()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        using (var initial = await Request(client)) Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        clock.Advance(TimeSpan.FromSeconds(60));
        await using (var scope = database.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().EmailVerificationQuotas
                .Where(value => value.Key == "send24:" + user.Id.ToString("N"))
                .ExecuteUpdateAsync(update => update.SetProperty(value => value.Count, 20));
        using var blocked = await Request(client); Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.RetryAfter?.Delta > TimeSpan.FromHours(23));
        Assert.Single(mailbox.Messages);
    }

    [Fact]
    public async Task UniqueActiveIndexAndCheckConstraintsProtectChallengeState()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        using var sent = await Request(client); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        var existing = await Challenge(id);
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.EmailVerificationChallenges.Add(new(Guid.NewGuid(), user.Id, user.Email, user.Email.ToUpperInvariant(),
                new string('A', 64), "v1", existing.CreatedAt));
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        }
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE identity."EmailVerificationChallenges" SET "FailedAttempts" = 6 WHERE "Id" = {id}
                """));
            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Users.Where(value => value.Id == user.Id).ExecuteDeleteAsync();
            Assert.False(await db.EmailVerificationChallenges.AnyAsync(value => value.Id == id));
        }
    }

    [Fact]
    public async Task RetiringAMacKeyFailsClosedAndAReplacementHostRetainsTheSendQuota()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var original = Factory(mailbox, clock); using var initial = Client(original);
        var user = await RegisterLogin(initial);
        using var sent = await Request(initial); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        await using var changed = Factory(mailbox, clock, extra: services => services.PostConfigure<
            HomeMatch.Modules.Identity.Infrastructure.EmailVerification.EmailVerificationOptions>(options =>
            {
                options.Keys.Clear(); options.KeyId = "v2"; options.Keys["v2"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            }));
        using var client = Client(changed); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        using var invalid = await Verify(client, id, Code(mailbox.Messages.Last()));
        await AssertError(invalid, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        using var cooldown = await Request(client); Assert.Equal(HttpStatusCode.TooManyRequests, cooldown.StatusCode);
    }
}
