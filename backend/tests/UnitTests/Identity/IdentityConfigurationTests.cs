using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;

namespace HomeMatch.UnitTests.Identity;

public class IdentityConfigurationTests
{
    [Fact]
    public void GeneratedSigningKeyAndContractDefaultsAreAccepted()
    {
        var options = ValidJwtOptions();

        Assert.True(new JwtOptionsValidator().Validate(null, options).Succeeded);
        Assert.Equal(15, options.ExpirationMinutes);
        Assert.Equal(30, options.ClockSkewSeconds);
        Assert.True(new IdentitySessionOptionsValidator().Validate(null, new IdentitySessionOptions()).Succeeded);
        Assert.Equal(24, new IdentitySessionOptions().NormalLifetimeHours);
        Assert.Equal(30, new IdentitySessionOptions().RememberLifetimeDays);
    }

    [Theory]
    [InlineData("")]
    [InlineData("super_secret_jwt_key_with_at_least_32_characters_long_123!")]
    [InlineData("<generate-a-random-key>")]
    [InlineData("not-base64-private-marker")]
    public void InvalidSecretIsRejectedWithoutIncludingValueInFailure(string secret)
    {
        var options = ValidJwtOptions();
        options.Secret = secret;

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Jwt:Secret", result.FailureMessage);
        if (secret.Length > 0)
        {
            Assert.DoesNotContain(secret, result.FailureMessage);
        }
    }

    [Fact]
    public void ShortOrTrivialDecodedKeyIsRejected()
    {
        var options = ValidJwtOptions();
        options.Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(31));
        Assert.True(new JwtOptionsValidator().Validate(null, options).Failed);

        options.Secret = Convert.ToBase64String(new byte[32]);
        Assert.True(new JwtOptionsValidator().Validate(null, options).Failed);
    }

    [Theory]
    [InlineData("issuer", "")]
    [InlineData("issuer", "  ")]
    [InlineData("audience", "")]
    [InlineData("audience", "  ")]
    public void MissingTokenIdentifierIsRejected(string field, string value)
    {
        var options = ValidJwtOptions();
        if (field == "issuer")
        {
            options.Issuer = value;
        }
        else
        {
            options.Audience = value;
        }

        Assert.True(new JwtOptionsValidator().Validate(null, options).Failed);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(16, 30)]
    [InlineData(15, -1)]
    [InlineData(15, 31)]
    public void TokenLifetimeOrSkewOutsidePolicyIsRejected(int minutes, int skew)
    {
        var options = ValidJwtOptions();
        options.ExpirationMinutes = minutes;
        options.ClockSkewSeconds = skew;

        Assert.True(new JwtOptionsValidator().Validate(null, options).Failed);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(25, 30)]
    [InlineData(24, 0)]
    [InlineData(24, 31)]
    public void SessionLifetimeOutsidePolicyIsRejected(int hours, int days)
    {
        var options = new IdentitySessionOptions { NormalLifetimeHours = hours, RememberLifetimeDays = days };

        Assert.True(new IdentitySessionOptionsValidator().Validate(null, options).Failed);
    }

    private static JwtOptions ValidJwtOptions() => new()
    {
        Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        Issuer = "HomeMatch.Tests",
        Audience = "HomeMatch.Tests.Client"
    };
}
