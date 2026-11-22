using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using HomeMatch.Modules.Identity.Application.Features.Profile.ChangePassword;
using HomeMatch.Modules.Identity.Application.Features.Profile.GetCurrentUser;
using HomeMatch.Modules.Identity.Application.Features.Profile.NotificationSettings;
using HomeMatch.Modules.Identity.Application.Features.Profile.UpdateProfile;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Profile.Requests;
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
            if (!TryGetUserContext(httpContext, out var userId, out var securityVersion))
            {
                return Results.Json(ApiResponse.Failure<CurrentUserProfileResponse>(
                    new Error(IdentityErrors.AuthenticationRequired, "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetCurrentUserQuery(userId, securityVersion);
            var result = await handler.HandleAsync(query, cancellationToken);

            if (result.IsFailure)
            {
                return Results.Json(ApiResponse.Failure<CurrentUserProfileResponse>(result.Error),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(ApiResponse.Success(MapProfileResponse(result.Value)));
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Lấy thông tin hồ sơ của người dùng hiện tại (yêu cầu Bearer token)")
        .WithTags("Profile")
        .Produces<ApiResponse<CurrentUserProfileResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<CurrentUserProfileResponse>>(StatusCodes.Status401Unauthorized);

        // Mục 3.10.1: Cập nhật thông tin hồ sơ tài khoản (PUT /api/v1/identity/me)
        endpoints.MapPut("/me", async (
            [FromBody] UpdateProfileRequest request,
            [FromServices] UpdateProfileHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserContext(httpContext, out var userId, out var securityVersion))
            {
                return Results.Json(ApiResponse.Failure<CurrentUserProfileResponse>(
                    new Error(IdentityErrors.AuthenticationRequired, "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new UpdateProfileCommand(
                UserId: userId,
                FullName: request.FullName,
                PhoneNumber: request.PhoneNumber,
                DateOfBirth: request.DateOfBirth,
                Gender: request.Gender,
                TokenSecurityVersion: securityVersion);

            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.Json(
                    ApiResponse.Failure<CurrentUserProfileResponse>(result.Error),
                    statusCode: MapErrorStatusCode(result.Error.Code));
            }

            return Results.Ok(ApiResponse.Success(MapProfileResponse(result.Value)));
        })
        .RequireAuthorization()
        .WithName("UpdateCurrentUserProfile")
        .WithSummary("Cập nhật thông tin hồ sơ tài khoản của người dùng hiện tại")
        .WithTags("Profile")
        .Produces<ApiResponse<CurrentUserProfileResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<CurrentUserProfileResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<CurrentUserProfileResponse>>(StatusCodes.Status401Unauthorized);

        // Mục 3.10.2: Đổi mật khẩu tài khoản (POST /api/v1/identity/change-password)
        endpoints.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            [FromServices] ChangePasswordHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserContext(httpContext, out var userId, out var securityVersion))
            {
                return Results.Json(ApiResponse.Failure<ChangePasswordResponse>(
                    new Error(IdentityErrors.AuthenticationRequired, "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new ChangePasswordCommand(
                UserId: userId,
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword,
                ConfirmNewPassword: request.ConfirmNewPassword,
                TokenSecurityVersion: securityVersion);

            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.Json(
                    ApiResponse.Failure<ChangePasswordResponse>(result.Error),
                    statusCode: MapErrorStatusCode(result.Error.Code));
            }

            return Results.Ok(ApiResponse.Success(new ChangePasswordResponse(result.Value.Message)));
        })
        .RequireAuthorization()
        .WithName("ChangePassword")
        .WithSummary("Đổi mật khẩu tài khoản hiện tại")
        .WithTags("Profile")
        .Produces<ApiResponse<ChangePasswordResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ChangePasswordResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ChangePasswordResponse>>(StatusCodes.Status401Unauthorized);

        // Mục 3.10.3: Xem cấu hình thông báo tài khoản (GET /api/v1/identity/me/notification-settings)
        endpoints.MapGet("/me/notification-settings", async (
            [FromServices] GetNotificationSettingsHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserContext(httpContext, out var userId, out var securityVersion))
            {
                return Results.Json(ApiResponse.Failure<NotificationSettingsResponse>(
                    new Error(IdentityErrors.AuthenticationRequired, "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetNotificationSettingsQuery(userId, securityVersion),
                cancellationToken);

            if (result.IsFailure)
            {
                return Results.Json(
                    ApiResponse.Failure<NotificationSettingsResponse>(result.Error),
                    statusCode: MapErrorStatusCode(result.Error.Code));
            }

            return Results.Ok(ApiResponse.Success(new NotificationSettingsResponse(
                NotifyPush: result.Value.NotifyPush,
                UpdatedAt: result.Value.UpdatedAt)));
        })
        .RequireAuthorization()
        .WithName("GetNotificationSettings")
        .WithSummary("Lấy cấu hình thông báo của tài khoản hiện tại")
        .WithTags("Profile")
        .Produces<ApiResponse<NotificationSettingsResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<NotificationSettingsResponse>>(StatusCodes.Status401Unauthorized);

        // Mục 3.10.3: Cập nhật cấu hình thông báo tài khoản (PUT /api/v1/identity/me/notification-settings)
        endpoints.MapPut("/me/notification-settings", async (
            [FromBody] UpdateNotificationSettingsRequest request,
            [FromServices] UpdateNotificationSettingsHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserContext(httpContext, out var userId, out var securityVersion))
            {
                return Results.Json(ApiResponse.Failure<NotificationSettingsResponse>(
                    new Error(IdentityErrors.AuthenticationRequired, "Chưa xác thực danh tính người dùng.")),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new UpdateNotificationSettingsCommand(userId, request.NotifyPush, securityVersion),
                cancellationToken);

            if (result.IsFailure)
            {
                return Results.Json(
                    ApiResponse.Failure<NotificationSettingsResponse>(result.Error),
                    statusCode: MapErrorStatusCode(result.Error.Code));
            }

            return Results.Ok(ApiResponse.Success(new NotificationSettingsResponse(
                NotifyPush: result.Value.NotifyPush,
                UpdatedAt: result.Value.UpdatedAt)));
        })
        .RequireAuthorization()
        .WithName("UpdateNotificationSettings")
        .WithSummary("Bật hoặc tắt nhận thông báo liên quan đến tài khoản và hoạt động thuê nhà")
        .WithTags("Profile")
        .Produces<ApiResponse<NotificationSettingsResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<NotificationSettingsResponse>>(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static CurrentUserProfileResponse MapProfileResponse(CurrentUserResult result) =>
        new(
            Id: result.Id,
            Email: result.Email,
            EmailVerified: result.EmailVerified,
            Role: result.Role,
            FullName: result.FullName,
            PhoneNumber: result.PhoneNumber,
            AvatarUrl: result.AvatarUrl,
            DateOfBirth: result.DateOfBirth,
            Gender: result.Gender,
            NotifyPush: result.NotifyPush,
            CreatedAt: result.CreatedAt,
            UpdatedAt: result.UpdatedAt);

    private static bool TryGetUserContext(HttpContext httpContext, out long userId, out int? securityVersion)
    {
        securityVersion = null;
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !long.TryParse(userIdClaim, out userId) || userId <= 0)
        {
            userId = 0;
            return false;
        }

        var securityVersionClaim = httpContext.User.FindFirst("security_version")?.Value;
        if (int.TryParse(securityVersionClaim, out var parsedVersion))
        {
            securityVersion = parsedVersion;
        }

        return true;
    }

    private static int MapErrorStatusCode(string errorCode) => errorCode switch
    {
        IdentityErrors.AuthenticationRequired => StatusCodes.Status401Unauthorized,
        IdentityErrors.SessionInvalid => StatusCodes.Status401Unauthorized,
        IdentityErrors.AccountSuspended => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
