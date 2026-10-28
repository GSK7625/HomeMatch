using System.Globalization;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenIssuer
{
    public IssuedAccessToken Issue(AccessTokenSubject subject)
    {
        if (subject.UserId == Guid.Empty || subject.SessionId == Guid.Empty || subject.SecurityVersion <= 0
            || !AccountRoles.All.Contains(subject.Role))
            throw new ArgumentException("Invalid access token subject.", nameof(subject));

        var settings = options.Value;
        // JWT NumericDate uses whole UTC seconds; returned metadata must match exactly.
        var now = DateTimeOffset.FromUnixTimeSeconds(clock.GetUtcNow().ToUnixTimeSeconds());
        var sessionEnd = DateTimeOffset.FromUnixTimeSeconds(subject.SessionExpiresAt.ToUnixTimeSeconds());
        var expires = new[] { now.AddMinutes(settings.ExpirationMinutes), sessionEnd }.Min();
        if (expires <= now) throw new AccessTokenSessionExpiredException();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer, Audience = settings.Audience,
            IssuedAt = now.UtcDateTime, NotBefore = now.UtcDateTime, Expires = expires.UtcDateTime,
            TokenType = "JWT",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(settings.Secret)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject.UserId.ToString("D"),
                ["sid"] = subject.SessionId.ToString("D"),
                ["jti"] = Guid.NewGuid().ToString("D"),
                ["role"] = subject.Role,
                ["sv"] = subject.SecurityVersion.ToString(CultureInfo.InvariantCulture)
            }
        };
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new IssuedAccessToken(token, "Bearer", (int)(expires - now).TotalSeconds,
            expires.UtcDateTime, subject.SessionExpiresAt.UtcDateTime);
    }
}
