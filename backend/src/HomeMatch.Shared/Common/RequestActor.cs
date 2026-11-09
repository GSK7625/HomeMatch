using System.Security.Claims;

namespace HomeMatch.Shared.Common;

public static class RequestActor
{
    public static long? UserId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
            && long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub"), out var id) && id > 0
            ? id
            : null;

    public static long? LandlordId(ClaimsPrincipal user) =>
        user.IsInRole("Landlord") ? UserId(user) : null;

    public static long? TenantId(ClaimsPrincipal user) =>
        user.IsInRole("Tenant") ? UserId(user) : null;

    public static long? AdminId(ClaimsPrincipal user) =>
        user.IsInRole("Admin") ? UserId(user) : null;
}
