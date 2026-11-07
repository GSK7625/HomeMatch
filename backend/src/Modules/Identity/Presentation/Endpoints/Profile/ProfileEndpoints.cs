using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using HomeMatch.Modules.Identity.Application.Features.Profile.GetCurrentUser;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Profile.Responses;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Profile;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // UC-A11: Xem thông tin người dùng hiện tại (/me)
        endpoints.MapGet("/me", async (
            [FromServices] GetCurrentUserHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? httpContext.User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Results.Json(ApiResponse.Failure<CurrentUserProfileResponse>(
                    new Error("Identity.AuthenticationRequired", "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            int? securityVersion = null;
            var securityVersionClaim = httpContext.User.FindFirst("security_version")?.Value;
            if (int.TryParse(securityVersionClaim, out var parsedVersion))
            {
                securityVersion = parsedVersion;
            }

            var query = new GetCurrentUserQuery(userId, securityVersion);
            var result = await handler.HandleAsync(query, cancellationToken);

            if (result.IsFailure)
            {
                return Results.Json(ApiResponse.Failure<CurrentUserProfileResponse>(result.Error),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var response = new CurrentUserProfileResponse(
                Id: result.Value.Id,
                Email: result.Value.Email,
                EmailVerified: result.Value.EmailVerified,
                Role: result.Value.Role,
                FullName: result.Value.FullName,
                PhoneNumber: result.Value.PhoneNumber,
                AvatarUrl: result.Value.AvatarUrl,
                CreatedAt: result.Value.CreatedAt,
                UpdatedAt: result.Value.UpdatedAt);

            return Results.Ok(ApiResponse.Success(response));
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Lấy thông tin hồ sơ của người dùng hiện tại (yêu cầu Bearer token)")
        .WithTags("Profile")
        .Produces<ApiResponse<CurrentUserProfileResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<CurrentUserProfileResponse>>(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
