using System.Globalization;
using System.Security.Claims;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Domain;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

public static class AccessTokenClaims
{
    public static bool TryRead(ClaimsPrincipal principal, int maximumLifetimeMinutes, out AccessTokenIdentity? identity)
    {
        identity = null;
        string? Single(string type)
        {
            var values = principal.FindAll(type).Select(claim => claim.Value).ToArray();
            return values.Length == 1 ? values[0] : null;
        }

        if (!Guid.TryParseExact(Single("sub"), "D", out var userId) || userId == Guid.Empty
            || !Guid.TryParseExact(Single("sid"), "D", out var sessionId) || sessionId == Guid.Empty
            || !Guid.TryParseExact(Single("jti"), "D", out var tokenId) || tokenId == Guid.Empty
            || !long.TryParse(Single("sv"), NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version <= 0
            || !long.TryParse(Single("iat"), NumberStyles.None, CultureInfo.InvariantCulture, out var issued)
            || !long.TryParse(Single("nbf"), NumberStyles.None, CultureInfo.InvariantCulture, out var notBefore)
            || !long.TryParse(Single("exp"), NumberStyles.None, CultureInfo.InvariantCulture, out var expires)
            || issued != notBefore || issued < 0 || expires <= issued
            || expires > DateTimeOffset.MaxValue.ToUnixTimeSeconds()
            || expires - issued > maximumLifetimeMinutes * 60L)
            return false;

        var role = Single("role");
        if (role is null || !AccountRoles.All.Contains(role)) return false;
        identity = new AccessTokenIdentity(userId, sessionId, role, version, DateTimeOffset.FromUnixTimeSeconds(expires));
        return true;
    }
}
