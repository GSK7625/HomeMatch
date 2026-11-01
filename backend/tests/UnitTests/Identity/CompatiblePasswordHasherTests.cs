using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Passwords;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public sealed class CompatiblePasswordHasherTests
{
    private readonly IdentityAccount _user = new("hash@example.test", DateTimeOffset.UtcNow);
    private readonly CompatiblePasswordHasher _hasher = new(Options.Create(new PasswordHasherOptions()));

    [Fact]
    public void NewHashesPreserveLongUnicodeSuffixesAndUseRandomSalts()
    {
        var password = new string('ệ', 40) + "first";
        var hash = _hasher.HashPassword(_user, password);
        Assert.StartsWith("{bcrypt-sha384}$2", hash);
        Assert.True(hash.Length <= 255);
        Assert.NotEqual(hash, _hasher.HashPassword(_user, password));
        Assert.Equal(PasswordVerificationResult.Success, _hasher.VerifyHashedPassword(_user, hash, password));
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, hash, new string('ệ', 40) + "other"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyHashesAuthenticateAndRequestUpgrade(bool bcrypt)
    {
        const string password = "Legacy password 2026!";
        var hash = bcrypt ? BCrypt.Net.BCrypt.HashPassword(password, 4)
            : new PasswordHasher<IdentityAccount>().HashPassword(_user, password);
        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, _hasher.VerifyHashedPassword(_user, hash, password));
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, hash, "incorrect"));
    }

    [Fact]
    public void RawBcryptCannotAuthenticateTruncatedLongInputs()
    {
        var password = new string('a', 72);
        var hash = BCrypt.Net.BCrypt.HashPassword(password, 4);
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, hash, password + "suffix"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("$2b$invalid")]
    [InlineData("{bcrypt-sha384}$2b$invalid")]
    public void MalformedStoredHashesFailClosed(string hash)
        => Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, hash, "password"));
}
