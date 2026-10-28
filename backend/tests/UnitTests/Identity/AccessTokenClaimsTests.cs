using System.Security.Claims;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;

namespace HomeMatch.UnitTests.Identity;

public class AccessTokenClaimsTests
{
    [Theory]
    [InlineData("sub")]
    [InlineData("sid")]
    [InlineData("jti")]
    [InlineData("role")]
    [InlineData("sv")]
    [InlineData("iat")]
    [InlineData("nbf")]
    [InlineData("exp")]
    public void RequiredClaimsMustExistExactlyOnce(string claimType)
    {
        var claims = new[]
        {
            new Claim("sub", Guid.NewGuid().ToString("D")), new Claim("sid", Guid.NewGuid().ToString("D")),
            new Claim("jti", Guid.NewGuid().ToString("D")), new Claim("role", "Tenant"),
            new Claim("sv", "1"), new Claim("iat", "100"), new Claim("nbf", "100"), new Claim("exp", "1000")
        };
        var valid = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        Assert.True(AccessTokenClaims.TryRead(valid, 15, out _));
        var missing = new ClaimsPrincipal(new ClaimsIdentity(claims.Where(claim => claim.Type != claimType), "Bearer"));
        Assert.False(AccessTokenClaims.TryRead(missing, 15, out _));
        var duplicate = new ClaimsPrincipal(new ClaimsIdentity(claims.Append(claims.Single(claim => claim.Type == claimType)), "Bearer"));
        Assert.False(AccessTokenClaims.TryRead(duplicate, 15, out _));
    }
}
