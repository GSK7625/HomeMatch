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
        var userId = Guid.NewGuid();
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
        => Assert.Throws<ArgumentException>(() => new UserProfile(Guid.NewGuid(), name, DateTimeOffset.UtcNow));

    [Fact]
    public void InvalidUtf16IsRejectedBeforePersistence()
    {
        // Attribute metadata cannot preserve an unpaired surrogate; construct it at runtime.
        var name = new string(new[] { 'a', (char)0xD800 });
        Assert.Throws<ArgumentException>(() => new UserProfile(Guid.NewGuid(), name, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void NameLengthCountsUnicodeScalarsRatherThanUtf16Units()
    {
        var validName = string.Concat(Enumerable.Repeat("😀", 100));
        Assert.Equal(validName, new UserProfile(Guid.NewGuid(), validName, DateTimeOffset.UtcNow).FullName);
        Assert.Throws<ArgumentException>(() => new UserProfile(Guid.NewGuid(), validName + "a", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ProfileCannotExistWithoutAnAccountId()
        => Assert.Throws<ArgumentException>(() => new UserProfile(Guid.Empty, "Nguyễn An", DateTimeOffset.UtcNow));
}