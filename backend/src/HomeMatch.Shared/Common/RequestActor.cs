using System.Security.Claims;

namespace HomeMatch.Shared.Common;

public static class RequestActor
{
    public static long? LandlordId(ClaimsPrincipal user) =>
        user.IsInRole("LANDLORD") &&
        long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
}
