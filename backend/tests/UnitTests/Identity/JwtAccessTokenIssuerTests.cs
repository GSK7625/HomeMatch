using System.Security.Cryptography;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HomeMatch.UnitTests.Identity;

public class JwtAccessTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 1, 0, 0, TimeSpan.Zero);
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }

    [Theory]
    [InlineData(3600, 900)]
    [InlineData(40, 40)]
    public async Task IssuedTokenHasOnlyExpectedClaimsAndNeverExceedsSession(int sessionSeconds, int expectedSeconds)
    {
        var options = new JwtOptions
        {
            Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            Issuer = "test-issuer", Audience = "test-client", ExpirationMinutes = 15
        };
        var subject = new AccessTokenSubject(Guid.NewGuid(), Guid.NewGuid(), AccountRoles.Tenant, 4, Now.AddSeconds(sessionSeconds));
        var issuer = new JwtAccessTokenIssuer(Options.Create(options), new FixedClock());
        var issued = issuer.Issue(subject);
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        var token = handler.ReadJsonWebToken(issued.AccessToken);
        Assert.Equal("HS256", token.Alg);
        Assert.Equal("JWT", token.Typ);
        Assert.Equal(new[] { "aud", "exp", "iat", "iss", "jti", "nbf", "role", "sid", "sub", "sv" },
            token.Claims.Select(claim => claim.Type).Order());
        Assert.Equal(expectedSeconds, issued.ExpiresIn);
        Assert.Equal(Now.AddSeconds(expectedSeconds).UtcDateTime, issued.ExpiresAt);
        Assert.Equal(issued.ExpiresAt, token.ValidTo);
        Assert.Equal(subject.SessionExpiresAt.UtcDateTime, issued.SessionExpiresAt);
        Assert.NotEqual(token.Id, handler.ReadJsonWebToken(issuer.Issue(subject).AccessToken).Id);

        var result = await handler.ValidateTokenAsync(issued.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = options.Issuer,
            ValidateAudience = true, ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(options.Secret)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            // Fixed test date need not be the machine clock; assert exact lifetime above.
            ValidateLifetime = false
        });
        Assert.True(result.IsValid);
        Assert.True(AccessTokenClaims.TryRead(new System.Security.Claims.ClaimsPrincipal(result.ClaimsIdentity), 15, out var read));
        Assert.Equal(subject.UserId, read!.UserId);
        Assert.Equal(subject.SessionId, read.SessionId);
        Assert.Equal(subject.SecurityVersion, read.SecurityVersion);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("role")]
    [InlineData("user")]
    [InlineData("session")]
    [InlineData("version")]
    public void InvalidSubjectCannotIssueToken(string invalid)
    {
        var settings = Options.Create(new JwtOptions { Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) });
        var issuer = new JwtAccessTokenIssuer(settings, new FixedClock());
        var failure = Assert.ThrowsAny<ArgumentException>(() => issuer.Issue(new AccessTokenSubject(
            invalid == "user" ? Guid.Empty : Guid.NewGuid(),
            invalid == "session" ? Guid.Empty : Guid.NewGuid(),
            invalid == "role" ? "admin" : AccountRoles.Tenant,
            invalid == "version" ? 0 : 1,
            invalid == "expired" ? Now : Now.AddDays(1))));
        if (invalid == "expired") Assert.IsType<AccessTokenSessionExpiredException>(failure);
    }
}
