using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Entities;

namespace HomeMatch.UnitTests.Identity;

public class IdentityDomainTests
{
    [Theory]
    [InlineData(AccountRoles.Tenant, true)]
    [InlineData(AccountRoles.Landlord, true)]
    [InlineData(AccountRoles.Admin, false)]
    [InlineData("tenant", false)]
    [InlineData(null, false)]
    public void PublicRegistrationOnlyAcceptsExplicitBusinessRoles(string? role, bool expected)
        => Assert.Equal(expected, AccountRoles.CanSelfRegister(role));

    [Fact]
    public void ProfileTrimsNameAndStoresUtcWithoutRequiringAPhone()
    {
        var created = new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(7));
        var userId = 1L;
        var profile = new UserProfile(userId, "  Nguyễn Văn An  ", created);

        Assert.Equal(userId, profile.UserId);
        Assert.Equal("Nguyễn Văn An", profile.FullName);
        Assert.Null(profile.PhoneNumber);
        Assert.Equal(TimeSpan.Zero, profile.CreatedAt.Offset);
        Assert.Equal(created.ToUniversalTime(), profile.CreatedAt);
        Assert.Equal(profile.CreatedAt, profile.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public void InvalidNameIsRejected(string name)
        => Assert.Throws<ArgumentException>(() => new UserProfile(999999L, name, DateTimeOffset.UtcNow));

    [Fact]
    public void InvalidUtf16IsRejectedBeforePersistence()
    {
        // Attribute metadata cannot preserve an unpaired surrogate; construct it at runtime.
        var name = new string(new[] { 'a', (char)0xD800 });
        Assert.Throws<ArgumentException>(() => new UserProfile(999999L, name, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void NameLengthCountsUnicodeScalarsRatherThanUtf16Units()
    {
        var validName = string.Concat(Enumerable.Repeat("😀", 100));
        Assert.Equal(validName, new UserProfile(999999L, validName, DateTimeOffset.UtcNow).FullName);
        Assert.Throws<ArgumentException>(() => new UserProfile(999999L, validName + "a", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ProfileCannotExistWithoutAnAccountId()
        => Assert.Throws<ArgumentException>(() => new UserProfile(0L, "Nguyễn An", DateTimeOffset.UtcNow));

    private static readonly DateTimeOffset Now = new(2026, 10, 28, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SessionUsesAbsoluteExpiryAndRevocationCannotBeUndone()
    {
        var session = new AccountSession(1L, 3, Now, Now.AddDays(30));
        Assert.NotEqual(Guid.Empty, session.Id);
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
            invalid == "user" ? 0L : 1L, invalid == "version" ? 0 : 1,
            Now, invalid == "expiry" ? Now : Now.AddDays(1)));
}
