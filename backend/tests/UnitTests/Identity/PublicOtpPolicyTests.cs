using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Application.PublicOtp;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Shared.Common;

namespace HomeMatch.UnitTests.Identity;

public sealed class PublicOtpPolicyTests
{
    [Fact]
    public void ChallengeClosesOnFifthWrongCodeAndRejectsFurtherAttempts()
    {
        var now = DateTimeOffset.UtcNow;
        var challenge = new PublicOtpChallenge { CreatedAt = now, ExpiresAt = now.AddMinutes(5), Submitted = true };
        for (var attempt = 0; attempt < 5; attempt++) challenge.RejectCode(now);
        Assert.True(challenge.Closed);
        Assert.Equal(5, challenge.FailedAttempts);
        Assert.False(challenge.CanVerifyAt(now));
        Assert.Throws<InvalidOperationException>(() => challenge.RejectCode(now));
    }

    [Fact]
    public void PendingAndExpiredChallengesCannotBeVerified()
    {
        var now = DateTimeOffset.UtcNow;
        var challenge = new PublicOtpChallenge { CreatedAt = now, ExpiresAt = now.AddMinutes(5) };
        Assert.False(challenge.CanVerifyAt(now));
        challenge.Submitted = true;
        Assert.True(challenge.CanVerifyAt(now));
        Assert.False(challenge.CanVerifyAt(now.AddMinutes(5)));
        Assert.False(challenge.CanVerifyAt(now.AddSeconds(-1)));
    }

    [Theory]
    [InlineData("invalid", "Registration")]
    [InlineData("test@example.test", "OtherPurpose")]
    public async Task SendRejectsInvalidBusinessInputBeforePersistence(string email, string purpose)
    {
        var store = new ProbeStore();
        var result = await new SendPublicOtpHandler(store).HandleAsync(email, purpose, "Password123", CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(PublicOtpErrors.Validation, result.Error);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task WeakNewPasswordNeverReachesPersistence()
    {
        var store = new ProbeStore();
        var result = await new ResetPasswordHandler(store, new NewPasswordPolicy()).HandleAsync("token", "weak", CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(0, store.Calls);
    }

    private sealed class ProbeStore : IPublicOtpStore
    {
        public int Calls { get; private set; }
        private Task<Result<PublicOtpResponse>> Called()
        {
            Calls++;
            return Task.FromResult(Result.Success(new PublicOtpResponse("Submitted")));
        }
        public Task<Result<PublicOtpResponse>> SendAsync(string email, string purpose, string? password, CancellationToken cancellationToken) => Called();
        public Task<Result<PublicOtpResponse>> VerifyAsync(Guid challengeId, string code, string purpose, CancellationToken cancellationToken) => Called();
        public Task<Result<PublicOtpResponse>> ResetAsync(string token, string password, CancellationToken cancellationToken) => Called();
    }
}
