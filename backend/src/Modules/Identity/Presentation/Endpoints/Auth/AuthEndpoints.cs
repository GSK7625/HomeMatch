using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using HomeMatch.Modules.Identity.Application.Features.Auth.ConfirmEmail;
using HomeMatch.Modules.Identity.Application.Features.Auth.ForgotPassword;
using HomeMatch.Modules.Identity.Application.Features.Auth.Login;
using HomeMatch.Modules.Identity.Application.Features.Auth.Logout;
using HomeMatch.Modules.Identity.Application.Features.Auth.LogoutAll;
using HomeMatch.Modules.Identity.Application.Features.Auth.RefreshToken;
using HomeMatch.Modules.Identity.Application.Features.Auth.Register;
using HomeMatch.Modules.Identity.Application.Features.Auth.ResendConfirmation;
using HomeMatch.Modules.Identity.Application.Features.Auth.ResetPassword;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Responses;
using HomeMatch.Modules.Identity.Presentation.Security;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Auth");

        // UC-A01: Đăng ký tài khoản
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            [FromServices] RegisterHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterCommand(
                request.Email,
                request.Password,
                request.FullName,
                request.Role);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.Failure<RegisterResponse>(result.Error));
            }

            var response = new RegisterResponse(result.Value.Message);
            return Results.Accepted(value: ApiResponse.Success(response));
        })
        .RequireRateLimiting(RateLimitingSetup.RegisterPolicy)
        .WithName("Register")
        .WithSummary("Đăng ký tài khoản người dùng mới (Tenant hoặc Landlord)")
        .Produces<ApiResponse<RegisterResponse>>(StatusCodes.Status202Accepted)
        .Produces<ApiResponse<RegisterResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status429TooManyRequests);

        // UC-A02: Xác thực email (POST)
        group.MapPost("/confirm-email", async (
            [FromBody] ConfirmEmailRequest request,
            [FromServices] ConfirmEmailHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ConfirmEmailCommand(request.UserId, request.Token);
            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.Failure<ConfirmEmailResponse>(result.Error));
            }

            var response = new ConfirmEmailResponse(result.Value.Message);
            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("ConfirmEmailPost")
        .WithSummary("Xác thực địa chỉ email bằng UserId và Token (POST body)")
        .Produces<ApiResponse<ConfirmEmailResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ConfirmEmailResponse>>(StatusCodes.Status400BadRequest);

        // UC-A02: Xác thực email trực tiếp từ link email (GET)
        group.MapGet("/confirm-email", async (
            [FromQuery] long userId,
            [FromQuery] string token,
            [FromServices] ConfirmEmailHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ConfirmEmailCommand(userId, token);
            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.Failure<ConfirmEmailResponse>(result.Error));
            }

            var response = new ConfirmEmailResponse(result.Value.Message);
            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("ConfirmEmailGet")
        .WithSummary("Xác thực địa chỉ email trực tiếp khi click link từ email (GET query string)")
        .Produces<ApiResponse<ConfirmEmailResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ConfirmEmailResponse>>(StatusCodes.Status400BadRequest);

        // UC-A03: Gửi lại email kích hoạt
        group.MapPost("/resend-confirmation", async (
            [FromBody] ResendConfirmationRequest request,
            [FromServices] ResendConfirmationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResendConfirmationCommand(request.Email);
            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.Failure<ResendConfirmationResponse>(result.Error));
            }

            var response = new ResendConfirmationResponse(result.Value.Message);
            return Results.Ok(ApiResponse.Success(response));
        })
        .RequireRateLimiting(RateLimitingSetup.RegisterPolicy)
        .WithName("ResendConfirmation")
        .WithSummary("Gửi lại email xác thực kích hoạt tài khoản")
        .Produces<ApiResponse<ResendConfirmationResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ResendConfirmationResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status429TooManyRequests);

        // UC-A04: Đăng nhập (Login)
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            [FromServices] LoginHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var deviceInfo = httpContext.Request.Headers.UserAgent.ToString();
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            var command = new LoginCommand(
                request.Email,
                request.Password,
                deviceInfo,
                ipAddress);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                var errorCode = result.Error.Code;
                if (errorCode == IdentityErrors.InvalidCredentials)
                {
                    return Results.Json(ApiResponse.Failure<IssuedAccessTokenResponse>(result.Error), statusCode: StatusCodes.Status401Unauthorized);
                }

                if (errorCode == IdentityErrors.EmailNotVerified || errorCode == IdentityErrors.AccountSuspended)
                {
                    return Results.Json(ApiResponse.Failure<IssuedAccessTokenResponse>(result.Error), statusCode: StatusCodes.Status403Forbidden);
                }

                return Results.BadRequest(ApiResponse.Failure<IssuedAccessTokenResponse>(result.Error));
            }

            // Đặt Refresh Token vào Cookie HttpOnly (SameSite=Lax)
            CookieAuthHelper.SetRefreshTokenCookie(httpContext, result.Value.RefreshToken, result.Value.SessionExpiresAt);

            var response = new IssuedAccessTokenResponse(
                AccessToken: result.Value.AccessToken,
                TokenType: "Bearer",
                ExpiresIn: result.Value.ExpiresIn,
                ExpiresAt: result.Value.ExpiresAt,
                SessionExpiresAt: result.Value.SessionExpiresAt);

            return Results.Ok(ApiResponse.Success(response));
        })
        .RequireRateLimiting(RateLimitingSetup.LoginPolicy)
        .WithName("Login")
        .WithSummary("Đăng nhập hệ thống bằng email và mật khẩu (nhận JWT và Refresh Cookie)")
        .Produces<ApiResponse<IssuedAccessTokenResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<IssuedAccessTokenResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<IssuedAccessTokenResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<IssuedAccessTokenResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status429TooManyRequests);

        // UC-A10: Làm mới Access Token (Refresh Token)
        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequest? bodyRequest,
            [FromServices] RefreshTokenHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var rawToken = CookieAuthHelper.GetRefreshTokenFromCookie(httpContext)
                           ?? bodyRequest?.RefreshToken;

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                CookieAuthHelper.ClearRefreshTokenCookie(httpContext);
                var error = new Error(IdentityErrors.SessionInvalid, "Không tìm thấy refresh token trong cookie hoặc request body.");
                return Results.Json(ApiResponse.Failure<IssuedAccessTokenResponse>(error), statusCode: StatusCodes.Status401Unauthorized);
            }

            var deviceInfo = httpContext.Request.Headers.UserAgent.ToString();
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            var command = new RefreshTokenCommand(rawToken, deviceInfo, ipAddress);
            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                CookieAuthHelper.ClearRefreshTokenCookie(httpContext);
                return Results.Json(ApiResponse.Failure<IssuedAccessTokenResponse>(result.Error), statusCode: StatusCodes.Status401Unauthorized);
            }

            // Cập nhật Refresh Cookie mới (Token Rotation)
            CookieAuthHelper.SetRefreshTokenCookie(httpContext, result.Value.RefreshToken, result.Value.SessionExpiresAt);

            var response = new IssuedAccessTokenResponse(
                AccessToken: result.Value.AccessToken,
                TokenType: "Bearer",
                ExpiresIn: result.Value.ExpiresIn,
                ExpiresAt: result.Value.ExpiresAt,
                SessionExpiresAt: result.Value.SessionExpiresAt);

            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("RefreshToken")
        .WithSummary("Làm mới Access Token bằng Refresh Token (Token Rotation)")
        .Produces<ApiResponse<IssuedAccessTokenResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<IssuedAccessTokenResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        // UC-A08: Đăng xuất phiên hiện tại (Logout)
        group.MapPost("/logout", async (
            [FromServices] LogoutHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var rawToken = CookieAuthHelper.GetRefreshTokenFromCookie(httpContext);
            var command = new LogoutCommand(rawToken);

            await handler.HandleAsync(command, cancellationToken);
            CookieAuthHelper.ClearRefreshTokenCookie(httpContext);

            return Results.NoContent();
        })
        .WithName("Logout")
        .WithSummary("Đăng xuất phiên hiện tại và xóa Refresh Cookie")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        // UC-A09: Đăng xuất tất cả phiên (Logout All)
        group.MapPost("/logout-all", async (
            [FromServices] LogoutAllHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? httpContext.User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Results.Json(ApiResponse.Failure<object>(new Error(IdentityErrors.AuthenticationRequired, "Chưa xác thực người dùng.")), statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new LogoutAllCommand(userId);
            await handler.HandleAsync(command, cancellationToken);
            CookieAuthHelper.ClearRefreshTokenCookie(httpContext);

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("LogoutAll")
        .WithSummary("Đăng xuất tất cả các phiên đăng nhập của tài khoản (yêu cầu Bearer token)")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized);

        // UC-A05: Yêu cầu khôi phục mật khẩu (Forgot Password)
        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            [FromServices] ForgotPasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ForgotPasswordCommand(request.Email);
            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.Failure<ForgotPasswordResponse>(result.Error));
            }

            var response = new ForgotPasswordResponse(result.Value.Message);
            return Results.Ok(ApiResponse.Success(response));
        })
        .RequireRateLimiting(RateLimitingSetup.ForgotPasswordPolicy)
        .WithName("ForgotPassword")
        .WithSummary("Yêu cầu gửi liên kết đặt lại mật khẩu qua email")
        .Produces<ApiResponse<ForgotPasswordResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ForgotPasswordResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status429TooManyRequests);

        // UC-A06 & UC-A07: Đặt lại mật khẩu mới (Reset Password)
        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            [FromServices] ResetPasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword);
            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.BadRequest(ApiResponse.Failure<ResetPasswordResponse>(result.Error));
            }

            var response = new ResetPasswordResponse(result.Value.Message);
            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("ResetPassword")
        .WithSummary("Đặt lại mật khẩu mới bằng UserId, Token và Mật khẩu mới")
        .Produces<ApiResponse<ResetPasswordResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ResetPasswordResponse>>(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
