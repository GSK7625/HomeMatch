using HomeMatch.Modules.Identity.Domain.Entities;

namespace HomeMatch.UnitTests.Identity;

public class AccountSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SessionUsesAbsoluteExpiryAndRevocationCannotBeUndone()
    {
        var session = new AccountSession(Guid.NewGuid(), 3, true, Now, Now.AddDays(30));
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.True(session.RememberMe);
        Assert.Equal(3, session.SecurityVersion);
        Assert.False(session.IsActiveAt(Now.AddTicks(-1)));
        Assert.True(session.IsActiveAt(Now));
        Assert.True(session.IsActiveAt(Now.AddDays(30).AddTicks(-1)));
        Assert.False(session.IsActiveAt(Now.AddDays(30)));
        session.Revoke(Now.AddHours(1));
        session.Revoke(Now.AddHours(2));
        Assert.Equal(Now.AddHours(1), session.RevokedAt);
        Assert.False(session.IsActiveAt(Now.AddHours(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Revoke(Now.AddTicks(-1)));
    }

    [Theory]
    [InlineData("user")]
    [InlineData("version")]
    [InlineData("expiry")]
    public void InvalidSessionStateIsRejected(string invalid)
        => Assert.ThrowsAny<ArgumentException>(() => new AccountSession(
            invalid == "user" ? Guid.Empty : Guid.NewGuid(), invalid == "version" ? 0 : 1,
            false, Now, invalid == "expiry" ? Now : Now.AddDays(1)));
}
