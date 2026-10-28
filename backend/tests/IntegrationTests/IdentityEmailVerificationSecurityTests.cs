using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

public partial class IdentityEmailVerificationTests
{
    [Theory]
    [InlineData("/auth/resend-verification")]
    [InlineData("/auth/verify-email")]
    public async Task OtpEndpointsRequireBearerAndNeverUseCookiesAsIdentity(string route)
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        await RegisterLogin(client);
        client.DefaultRequestHeaders.Authorization = null;
        using var response = await client.PostAsync(Root + route, null);
        await AssertError(response, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
        Assert.Empty(mailbox.Messages);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"email\":\"other@example.test\"}")]
    [InlineData("{\"userId\":\"00000000-0000-0000-0000-000000000001\"}")]
    public async Task RequestRejectsFieldsThatCouldSelectAnotherAccount(string body)
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        await RegisterLogin(client);
        using var response = await client.PostAsync(Root + "/auth/resend-verification", new StringContent(body, Encoding.UTF8, "application/json"));
        await AssertError(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
        Assert.Empty(mailbox.Messages);
    }

    [Theory]
    [InlineData("{\"challengeId\":\"00000000-0000-0000-0000-000000000001\",\"code\":123456}")]
    [InlineData("{\"challengeId\":\"00000000-0000-0000-0000-000000000001\",\"code\":\"１２３４５６\"}")]
    [InlineData("{\"challengeId\":\"00000000-0000-0000-0000-000000000001\",\"code\":\" 12345\"}")]
    [InlineData("{\"challengeId\":\"00000000-0000-0000-0000-000000000001\",\"code\":\"123456\",\"code\":\"123456\"}")]
    [InlineData("{\"challengeId\":\"bad\",\"code\":\"123456\"}")]
    [InlineData("{}")]
    public async Task VerifyRejectsCoercionDuplicateKeysAndMalformedInput(string body)
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        await RegisterLogin(client);
        using var response = await client.PostAsync(Root + "/auth/verify-email", new StringContent(body, Encoding.UTF8, "application/json"));
        await AssertError(response, HttpStatusCode.BadRequest, "Identity.ValidationFailed");
        Assert.Empty(mailbox.Messages);
    }

    [Fact]
    public async Task RecipientOwnershipIsCheckedAndEmailSnapshotChangesCannotVerifyOldAddress()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var first = Client(factory); using var second = Client(factory);
        var owner = await RegisterLogin(first); await RegisterLogin(second);
        using var sent = await Request(first); var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        var code = Code(mailbox.Messages.Last());
        using var wrongOwner = await Verify(second, id, code);
        await AssertError(wrongOwner, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        Assert.Equal(0, (await Challenge(id)).FailedAttempts);
        await using (var scope = database.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(value => value.Id == owner.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(value => value.Email, "changed@example.test")
                    .SetProperty(value => value.NormalizedEmail, "CHANGED@EXAMPLE.TEST"));
        using var changed = await Verify(first, id, code);
        await AssertError(changed, HttpStatusCode.BadRequest, "Identity.EmailVerificationInvalid");
        Assert.Equal(0, (await Challenge(id)).FailedAttempts);
    }

    [Fact]
    public async Task DisabledOtpDoesNotBreakLoginAndDoesNotCreateChallenges()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock, enabled: false); using var client = Client(factory);
        var user = await RegisterLogin(client);
        using var response = await Request(client);
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
        Assert.Empty(mailbox.Messages);
        await using var scope = database.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().EmailVerificationChallenges.AnyAsync(value => value.UserId == user.Id));
    }

    [Fact]
    public async Task OtpMediaAndBodySizeErrorsHaveNoMailSideEffects()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        await RegisterLogin(client);
        using var media = await client.PostAsync(Root + "/auth/resend-verification", new StringContent("{}"));
        await AssertError(media, HttpStatusCode.UnsupportedMediaType, "Identity.UnsupportedMediaType");
        using var size = await client.PostAsync(Root + "/auth/verify-email", new StringContent(new string('x', 16 * 1024 + 1), Encoding.UTF8, "application/json"));
        await AssertError(size, HttpStatusCode.RequestEntityTooLarge, "Identity.RequestTooLarge");
        Assert.Empty(mailbox.Messages);
    }

    [Fact]
    public async Task UnavailableDatabaseDoesNotSubmitMail()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var original = Factory(mailbox, clock); using var first = Client(original);
        var user = await RegisterLogin(first);
        await using var unavailable = Factory(mailbox, clock, availableDatabase: false); using var client = Client(unavailable);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        using var response = await Request(client);
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
        Assert.Empty(mailbox.Messages);
    }

    [Fact]
    public async Task PersistentQuotaSurvivesNewHostAndResendDoesNotResetVerifyQuota()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        for (var index = 0; index < 20; index++)
        {
            using var invalid = await Verify(client, Guid.NewGuid(), "000000");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var sent = await Request(client); Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var id = (await Data(sent)).GetProperty("challengeId").GetGuid();
        await using var restarted = Factory(mailbox, clock); using var second = Client(restarted);
        second.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        using var limited = await Verify(second, id, Code(mailbox.Messages.Last()));
        await AssertError(limited, HttpStatusCode.TooManyRequests, "Identity.RateLimitExceeded");
        Assert.True(limited.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task SendingQuotaPersistsAcrossHostsAndFailedSubmissionsCountAgainstIt()
    {
        var mailbox = new Mailbox { Outcome = HomeMatch.Shared.Contracts.Notifications.EmailSubmissionOutcome.Unknown };
        var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        for (var index = 0; index < 5; index++)
        {
            using var sent = await Request(client); Assert.Equal(HttpStatusCode.ServiceUnavailable, sent.StatusCode);
            clock.Advance(TimeSpan.FromSeconds(60));
        }
        await using var restarted = Factory(mailbox, clock); using var second = Client(restarted);
        second.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        using var limited = await Request(second);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.Equal(5, mailbox.Messages.Count);
    }

    [Fact]
    public async Task AbandonedPendingLeaseCanBeReplacedAndCannotFinalizeOverItsSuccessor()
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        VerificationResult pending; VerificationSubject subject;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            subject = new(user.Id, (await db.Sessions.SingleAsync(value => value.UserId == user.Id)).Id, "Tenant");
            pending = await scope.ServiceProvider.GetRequiredService<IEmailVerificationStore>().ReserveAsync(subject, true, mailbox.SenderKey, default);
        }
        Assert.Equal(VerificationOutcome.Reserved, pending.Outcome);
        using var blocked = await Request(client); Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        clock.Advance(TimeSpan.FromMinutes(2));
        using var sent = await Request(client); Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var stale = await scope.ServiceProvider.GetRequiredService<IEmailVerificationStore>()
                .CompleteAsync(subject, pending.ChallengeId!.Value, VerificationDeliveryOutcome.Accepted, default);
            Assert.Equal(VerificationOutcome.Unavailable, stale.Outcome);
        }
        Assert.Equal(EmailVerificationState.Invalidated, (await Challenge(pending.ChallengeId!.Value)).State);
        using var verified = await Verify(client, (await Data(sent)).GetProperty("challengeId").GetGuid(), Code(mailbox.Messages.Last()));
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
    }

    [Theory]
    [InlineData(false, 30)]
    [InlineData(true, 120)]
    public async Task IpGuardCountsMalformedRequestsAndIgnoresUntrustedForwardedFor(bool verify, int limit)
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        await RegisterLogin(client);
        var route = verify ? "/auth/verify-email" : "/auth/resend-verification";
        for (var index = 0; index < limit; index++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Root + route);
            request.Headers.Add("X-Forwarded-For", "198.51.100." + index);
            request.Content = new StringContent(verify ? "{}" : "{\"userId\":\"bad\"}", Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var blocked = await client.PostAsync(Root + route, null);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        Assert.Empty(mailbox.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmissionCannotActivateAChallengeAfterExpiryOrEmailChange(bool changeEmail)
    {
        var mailbox = new Mailbox(); var clock = new Clock();
        await using var factory = Factory(mailbox, clock); using var client = Client(factory);
        var user = await RegisterLogin(client);
        mailbox.BeforeReturn = async _ =>
        {
            if (!changeEmail) clock.Advance(TimeSpan.FromMinutes(5));
            else
            {
                await using var scope = database.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(value => value.Id == user.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(value => value.Email, "changed-finalize@example.test")
                        .SetProperty(value => value.NormalizedEmail, "CHANGED-FINALIZE@EXAMPLE.TEST"));
            }
        };
        using var response = await Request(client);
        await AssertError(response, HttpStatusCode.ServiceUnavailable, "Server.DependencyUnavailable");
        await using var readScope = database.Services.CreateAsyncScope();
        var db = readScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var challenge = await db.EmailVerificationChallenges.SingleAsync(value => value.UserId == user.Id);
        Assert.Equal(EmailVerificationState.Invalidated, challenge.State);
        Assert.False((await db.Users.SingleAsync(value => value.Id == user.Id)).EmailConfirmed);
    }
}
