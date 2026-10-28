using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.EmailVerification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public sealed class EmailVerificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
    private static EmailVerificationChallenge Challenge() => new(Guid.NewGuid(), Guid.NewGuid(),
        "tenant@example.test", "TENANT@EXAMPLE.TEST", new string('A', 64), "v1", Now);

    [Fact]
    public void PendingCannotVerifyAndSubmitCannotExtendTheOriginalDeadline()
    {
        var challenge = Challenge();
        Assert.False(challenge.IsVerifiableAt(Now));
        challenge.Submit(Now.AddMinutes(1));
        Assert.Equal(Now.AddMinutes(5), challenge.ExpiresAt);
        Assert.True(challenge.IsVerifiableAt(Now.AddMinutes(4)));
        Assert.False(challenge.IsVerifiableAt(Now.AddMinutes(5)));
        Assert.Throws<InvalidOperationException>(() => challenge.Consume(Now.AddMinutes(5)));
    }

    [Fact]
    public void CorrectCodeCanConsumeOnlyOnce()
    {
        var challenge = Challenge(); challenge.Submit(Now); challenge.Consume(Now.AddSeconds(1));
        Assert.Equal(EmailVerificationState.Consumed, challenge.State);
        Assert.Throws<InvalidOperationException>(() => challenge.Consume(Now.AddSeconds(2)));
        Assert.Throws<InvalidOperationException>(() => challenge.RejectCode(Now.AddSeconds(2)));
    }

    [Fact]
    public void FiveWrongCodesLockOnlyTheChallenge()
    {
        var challenge = Challenge(); challenge.Submit(Now);
        for (var index = 0; index < 5; index++) challenge.RejectCode(Now.AddSeconds(index));
        Assert.Equal(5, challenge.FailedAttempts);
        Assert.Equal(EmailVerificationState.Locked, challenge.State);
        Assert.False(challenge.IsVerifiableAt(Now.AddSeconds(6)));
        Assert.Throws<InvalidOperationException>(() => challenge.RejectCode(Now.AddSeconds(6)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailedOrInvalidatedReservationsCannotBecomeSent(bool failed)
    {
        var challenge = Challenge(); challenge.EndSubmission(Now, failed);
        Assert.Equal(failed ? EmailVerificationState.Failed : EmailVerificationState.Invalidated, challenge.State);
        Assert.Throws<InvalidOperationException>(() => challenge.Submit(Now));
    }

    [Fact]
    public void SenderQuotaResetsAtUtcMidnightWhileUserWindowKeepsItsOwnDuration()
    {
        var now = Now.AddHours(23);
        var sender = new EmailVerificationQuota("sender", Now, Now.AddDays(1));
        sender.Consume(now, 1);
        Assert.Equal(TimeSpan.FromHours(1), sender.RetryAfter(now, 1));
        sender.RenewIfExpired(Now.AddDays(1), TimeSpan.FromDays(1), utcDay: true);
        sender.Consume(Now.AddDays(1), 1);
        var user = new EmailVerificationQuota("user", now, now.AddDays(1));
        Assert.Equal(now.AddDays(1), user.WindowEnd);
        Assert.Throws<InvalidOperationException>(() => sender.Consume(Now.AddDays(1), 1));
    }

    [Fact]
    public void MacBindsPurposeUserEmailAndChallengeAndSupportsRetainedKeys()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var settings = new EmailVerificationOptions { Enabled = true, Keys = new() { ["v1"] = key } };
        var protector = new EmailVerificationCodeProtector(Options.Create(settings));
        var user = Guid.NewGuid(); var id = Guid.NewGuid();
        var material = protector.Create(user, "TENANT@EXAMPLE.TEST", id);
        Assert.Matches("^[0-9]{6}$", material.Code);
        var challenge = new EmailVerificationChallenge(id, user, "tenant@example.test", "TENANT@EXAMPLE.TEST", material.Mac, material.KeyId, Now);
        Assert.True(protector.Verify(challenge, material.Code));
        Assert.False(protector.Verify(new(Guid.NewGuid(), user, challenge.EmailSnapshot, challenge.NormalizedEmailSnapshot, material.Mac, "v1", Now), material.Code));
        Assert.False(protector.Verify(new(id, Guid.NewGuid(), challenge.EmailSnapshot, challenge.NormalizedEmailSnapshot, material.Mac, "v1", Now), material.Code));
        Assert.False(protector.Verify(new(id, user, "other@example.test", "OTHER@EXAMPLE.TEST", material.Mac, "v1", Now), material.Code));
        settings.Keys["v2"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); settings.KeyId = "v2";
        Assert.True(protector.Verify(challenge, material.Code));
        settings.Keys.Remove("v1");
        Assert.False(protector.Verify(challenge, material.Code));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("１２３４５６")]
    [InlineData(" 12345")]
    public void MacVerifierDoesNotCoerceInvalidCodeStrings(string code)
    {
        var protector = new EmailVerificationCodeProtector(Options.Create(new EmailVerificationOptions()));
        Assert.False(protector.Verify(Challenge(), code));
    }

    [Fact]
    public void EnabledOtpRejectsMissingTrivialOrReusedJwtKeysButDisabledOtpStartsWithoutKeys()
    {
        var jwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var validator = new EmailVerificationOptionsValidator(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = jwt }).Build());
        var settings = new EmailVerificationOptions();
        Assert.True(validator.Validate(null, settings).Succeeded);
        settings.Enabled = true;
        Assert.True(validator.Validate(null, settings).Failed);
        settings.Keys["v1"] = jwt;
        Assert.True(validator.Validate(null, settings).Failed);
        settings.Keys["v1"] = Convert.ToBase64String(new byte[32]);
        Assert.True(validator.Validate(null, settings).Failed);
        settings.Keys["v1"] = "invalid";
        Assert.True(validator.Validate(null, settings).Failed);
        settings.Keys["v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Assert.True(validator.Validate(null, settings).Succeeded);
    }
}
