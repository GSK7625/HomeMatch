using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Infrastructure.Passwords;
using HomeMatch.Modules.Identity.Presentation.Registration;
using Microsoft.Extensions.Options;

namespace HomeMatch.UnitTests.Identity;

public class RegistrationPolicyTests
{
    private readonly NewPasswordPolicy _policy = new(new OfflinePasswordBlocklist());

    [Theory]
    [InlineData(" An+tag@Example.COM ", "An+tag@Example.COM", "AN+TAG@EXAMPLE.COM")]
    [InlineData("a.b@example.com", "a.b@example.com", "A.B@EXAMPLE.COM")]
    public void EmailKeepsDisplaySpellingAndDoesNotRemovePlusOrDots(string input, string display, string normalized)
    {
        Assert.True(AccountInput.TryNormalizeEmail(input, out var actual, out var key));
        Assert.Equal(display, actual);
        Assert.Equal(normalized, key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("a b@example.com")]
    [InlineData("An <an@example.com>")]
    [InlineData("a@example.com,b@example.com")]
    [InlineData("ân@example.com")]
    [InlineData("a..b@example.com")]
    [InlineData(".a@example.com")]
    [InlineData("a@.example.com")]
    public void InvalidEmailIsRejected(string? input)
        => Assert.False(AccountInput.TryNormalizeEmail(input, out _, out _));

    [Fact]
    public void EmailLimitAppliesAfterTrimming()
    {
        var tooLong = new string('a', 255) + "@example.com";
        Assert.False(AccountInput.TryNormalizeEmail(tooLong, out _, out _));
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void PasswordLengthCountsUnicodeScalars(int count, bool expected)
        => Assert.Equal(expected, _policy.IsAllowed(string.Concat(Enumerable.Repeat("😀", count))));

    [Theory]
    [InlineData("123456789012345")]
    [InlineData("1234567890123456")]
    public void EmbeddedCorpusBlocksKnownLongPasswords(string password)
    {
        Assert.True(new OfflinePasswordBlocklist().Contains(password));
        Assert.False(_policy.IsAllowed(password));
    }

    [Fact]
    public void PasswordIsNotTrimmedOrNormalizedAndInvalidUtf16IsRejected()
    {
        Assert.True(_policy.IsAllowed("  Mây xanh phủ đồi sáng 2026!  "));
        Assert.False(_policy.IsAllowed(null));
        Assert.False(_policy.IsAllowed(new string(new[] { 'a', (char)0xD800 }) + new string('b', 20)));
    }

    [Theory]
    [InlineData("An\nBình")]
    [InlineData("An\tBình")]
    [InlineData("An\0Bình")]
    public void ProfileRejectsControlCharacters(string input)
        => Assert.Throws<ArgumentException>(() => new UserProfile(Guid.NewGuid(), input, DateTimeOffset.UtcNow));

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static RegistrationRateLimiter Limiter(Clock clock) => new(Options.Create(new JwtOptions
        { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }), clock);

    [Fact]
    public void QuotaWindowsAreIndependentAndRejectedAcquisitionIsAtomic()
    {
        var clock = new Clock();
        var limiter = Limiter(clock);
        for (var index = 0; index < 3; index++) Assert.Null(limiter.TryAcquire("first-ip", "SAME@EXAMPLE.TEST"));
        Assert.Equal(TimeSpan.FromHours(1), limiter.TryAcquire("second-ip", "SAME@EXAMPLE.TEST"));
        // Email rejection must not spend the second IP's permits.
        for (var index = 0; index < 5; index++) Assert.Null(limiter.TryAcquire("second-ip", "OTHER-" + index));
        Assert.Equal(TimeSpan.FromMinutes(15), limiter.TryAcquire("second-ip", "ANOTHER"));
        Assert.Equal(TimeSpan.FromHours(1), limiter.TryAcquire("second-ip", "SAME@EXAMPLE.TEST"));
        clock.Now = clock.Now.AddMinutes(15);
        Assert.Equal(TimeSpan.FromMinutes(45), limiter.TryAcquire("second-ip", "SAME@EXAMPLE.TEST"));
        clock.Now = clock.Now.AddMinutes(45);
        Assert.Null(limiter.TryAcquire("second-ip", "SAME@EXAMPLE.TEST"));
    }

    [Fact]
    public async Task ConcurrentQuotaAcquisitionCannotExceedEmailAllowance()
    {
        var limiter = Limiter(new Clock());
        var results = await Task.WhenAll(Enumerable.Range(0, 30)
            .Select(index => Task.Run(() => limiter.TryAcquire("ip-" + index, "SAME@EXAMPLE.TEST"))));
        Assert.Equal(3, results.Count(result => result is null));
    }
}
