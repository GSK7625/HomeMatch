using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

public partial class IdentityEmailVerificationTests
{
    [Fact]
    public async Task SmtpWaitHoldsNoDatabaseLocksAndPendingCannotVerifyOrSendAgain()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mailbox = new Mailbox { BeforeReturn = async ct => { entered.TrySetResult(); await release.Task.WaitAsync(ct); } };
        var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        var first = Request(client);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using var blocked = await Request(client).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
            await using var scope = database.Services.CreateAsyncScope();
            var challenge = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().EmailVerificationChallenges.SingleAsync(value => value.UserId == user.Id);
            Assert.Equal(EmailVerificationState.Pending, challenge.State);
            using var pending = await Verify(client, challenge.Id, Code(mailbox.Messages.Last())).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(HttpStatusCode.BadRequest, pending.StatusCode);
        }
        finally { release.TrySetResult(); }
        using var sent = await first; Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.Single(mailbox.Messages);
    }

    [Fact]
    public async Task LogoutAllDuringSmtpPreventsFinalizingTheChallenge()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mailbox = new Mailbox { BeforeReturn = async ct => { entered.TrySetResult(); await release.Task.WaitAsync(ct); } };
        var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        var pending = Request(client);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using var logout = await client.PostAsync(Root + "/auth/logout-all", null).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        }
        finally { release.TrySetResult(); }
        using var result = await pending;
        await AssertError(result, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(EmailVerificationState.Invalidated, (await db.EmailVerificationChallenges.SingleAsync(value => value.UserId == user.Id)).State);
        Assert.False((await db.Users.SingleAsync(value => value.Id == user.Id)).EmailConfirmed);
    }

    [Fact]
    public async Task TwoCorrectVerificationsConsumeOnlyOnceUnderAccountLock()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var original = Factory(mailbox, clock); using var initial = Client(original);
        var user = await RegisterLogin(initial);
        using var sent = await Request(initial); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        var barrier = new PrincipalBarrier();
        await using var racing = Factory(mailbox, clock, extra: services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier)));
        using var client = Client(racing); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var results = await Task.WhenAll(Verify(client, id, Code(mailbox.Messages.Last())), Verify(client, id, Code(mailbox.Messages.Last())));
        using var first = results[0]; using var second = results[1];
        Assert.Equal(2, barrier.Arrivals);
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest }, results.Select(value => value.StatusCode).OrderBy(value => (int)value));
        Assert.Equal(EmailVerificationState.Consumed, (await Challenge(id)).State);
    }

    [Fact]
    public async Task ConcurrentWrongCodesDoNotLoseAttemptIncrements()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var original = Factory(mailbox, clock); using var initial = Client(original);
        var user = await RegisterLogin(initial);
        using var sent = await Request(initial); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        var barrier = new PrincipalBarrier();
        await using var racing = Factory(mailbox, clock, extra: services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier)));
        using var client = Client(racing); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var wrong = Wrong(Code(mailbox.Messages.Last()));
        var results = await Task.WhenAll(Verify(client, id, wrong), Verify(client, id, wrong));
        using var first = results[0]; using var second = results[1];
        Assert.All(results, value => Assert.Equal(HttpStatusCode.BadRequest, value.StatusCode));
        Assert.Equal(2, barrier.Arrivals); Assert.Equal(2, (await Challenge(id)).FailedAttempts);
    }

    [Fact]
    public async Task VerifyAndResendAreSerializedAndNeverVerifyTheSupersededChallenge()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var original = Factory(mailbox, clock); using var initial = Client(original);
        var user = await RegisterLogin(initial);
        using var sent = await Request(initial); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        var code = Code(mailbox.Messages.Last());
        clock.Advance(TimeSpan.FromSeconds(60));
        var barrier = new PrincipalBarrier();
        await using var racing = Factory(mailbox, clock, extra: services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier)));
        using var client = Client(racing); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var results = await Task.WhenAll(Verify(client, id, code), Request(client));
        using var verify = results[0]; using var resend = results[1];
        Assert.Equal(2, barrier.Admitted); Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
        if (verify.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("AlreadyVerified", (await Data(resend)).GetProperty("status").GetString());
            Assert.Equal(EmailVerificationState.Consumed, (await Challenge(id)).State);
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);
            Assert.Equal("Submitted", (await Data(resend)).GetProperty("status").GetString());
            Assert.Equal(EmailVerificationState.Invalidated, (await Challenge(id)).State);
        }
    }

    [Fact]
    public async Task VerifyAndLogoutAllRecheckThePrincipalAfterAuthentication()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var original = Factory(mailbox, clock); using var initial = Client(original);
        var user = await RegisterLogin(initial);
        using var sent = await Request(initial); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        var barrier = new PrincipalBarrier();
        await using var racing = Factory(mailbox, clock, extra: services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier)));
        using var client = Client(racing); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var results = await Task.WhenAll(Verify(client, id, Code(mailbox.Messages.Last())), client.PostAsync(Root + "/auth/logout-all", null));
        using var verify = results[0]; using var logout = results[1];
        Assert.Equal(2, barrier.Arrivals); Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(verify.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(verify.StatusCode == HttpStatusCode.OK, (await db.Users.SingleAsync(value => value.Id == user.Id)).EmailConfirmed);
        Assert.Equal(2L, (await db.Users.SingleAsync(value => value.Id == user.Id)).SecurityVersion);
    }

    [Theory]
    [InlineData(EmailVerificationState.Pending)]
    [InlineData(EmailVerificationState.Sent)]
    [InlineData(EmailVerificationState.Consumed)]
    public async Task FailureAfterSqlWritesRollsBackReservationFinalizationOrVerification(EmailVerificationState failState)
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var original = Factory(mailbox, clock); using var initial = Client(original);
        var user = await RegisterLogin(initial);
        Guid? id = null;
        if (failState == EmailVerificationState.Consumed)
        {
            using var sent = await Request(initial); id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        }
        var fault = new FailAfterSave(failState);
        await using var failing = Factory(mailbox, clock, extra: services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(fault)));
        using var client = Client(failing); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        using var response = failState == EmailVerificationState.Consumed
            ? await Verify(client, id!.Value, Code(mailbox.Messages.Last())) : await Request(client);
        await AssertError(response, HttpStatusCode.InternalServerError, "Server.InternalError");
        Assert.DoesNotContain("private-otp-fault-marker", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, fault.WritesObserved);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.False((await db.Users.SingleAsync(value => value.Id == user.Id)).EmailConfirmed);
        var challenge = await db.EmailVerificationChallenges.SingleOrDefaultAsync(value => value.UserId == user.Id);
        if (failState == EmailVerificationState.Pending)
        {
            Assert.Null(challenge); Assert.Empty(mailbox.Messages);
            Assert.False(await db.EmailVerificationQuotas.AnyAsync(value => value.Key == "sender:" + mailbox.SenderKey));
        }
        else
        {
            Assert.NotNull(challenge);
            Assert.Equal(failState == EmailVerificationState.Sent ? EmailVerificationState.Pending : EmailVerificationState.Sent, challenge.State);
            Assert.Null(challenge.ConsumedAt);
            Assert.Single(mailbox.Messages);
        }
    }

    private sealed class PrincipalBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => _arrivals;
        public int Admitted => Math.Min(_arrivals, 2);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM identity.\"Users\"", StringComparison.Ordinal)
                && command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal))
            {
                var count = Interlocked.Increment(ref _arrivals);
                if (count <= 2)
                {
                    if (count == 2) _ready.TrySetResult();
                    await _ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
                }
            }
            return result;
        }
    }

    private sealed class FailAfterSave(EmailVerificationState failState) : SaveChangesInterceptor
    {
        public int WritesObserved;
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<EmailVerificationChallenge>().Any(value => value.Entity.State == failState))
            {
                Interlocked.Increment(ref WritesObserved);
                throw new InvalidOperationException("private-otp-fault-marker");
            }
            return ValueTask.FromResult(result);
        }
    }
}
