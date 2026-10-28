using HomeMatch.Modules.Identity.Presentation.EmailVerification;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Presentation.Registration;
using HomeMatch.Modules.Identity.Presentation.Login;
using HomeMatch.Modules.Identity.Presentation.Refresh;
using HomeMatch.Modules.Identity.Presentation.Logout;
using HomeMatch.Modules.Identity.Presentation.Authentication;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Identity.Presentation;

public static class IdentityEndpoints
{
    public static void MapAccountEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/auth/register", RegistrationEndpoint.HandleAsync)
            .AllowAnonymous()
            .WithName("Identity.Register");

        group.MapPost("/auth/login", LoginEndpoint.HandleAsync)
            .AllowAnonymous()
            .WithName("Identity.Login");

        group.MapPost("/auth/refresh", RefreshEndpoint.HandleAsync)
            .AllowAnonymous()
            .WithName("Identity.Refresh");

        group.MapPost("/auth/logout", LogoutEndpoints.CurrentAsync)
            .AllowAnonymous()
            .WithName("Identity.Logout");

        group.MapPost("/auth/logout-all", LogoutEndpoints.AllAsync)
            .RequireAuthorization(IdentityPolicies.Authenticated)
            .WithName("Identity.LogoutAll");

        group.MapPost("/auth/resend-verification", EmailVerificationEndpoints.RequestAsync)
            .RequireAuthorization(IdentityPolicies.Authenticated).WithName("Identity.RequestEmailVerification");
        group.MapPost("/auth/verify-email", EmailVerificationEndpoints.VerifyAsync)
            .RequireAuthorization(IdentityPolicies.Authenticated).WithName("Identity.VerifyEmail");

        group.MapGet("/me", (ICurrentIdentity current) =>
                current.Identity is { } identity
                    ? Results.Ok(ApiResponse<CurrentUserProfile>.Success(identity.Profile))
                    : IdentityHttpErrors.AuthenticationRequired())
            .RequireAuthorization(IdentityPolicies.Authenticated)
            .RequireRateLimiting("Identity.Profile")
            .WithName("Identity.GetMyProfile");

        group.MapGet("/auth/csrf", (HttpContext context, IdentityBrowserSecurity security, IAntiforgery antiforgery) =>
            {
                if (!security.IsAllowedOrigin(context)) return IdentityHttpErrors.OriginNotAllowed();
                var tokens = security.Bootstrap(context, antiforgery);
                return Results.Ok(ApiResponse<CsrfResponse>.Success(new CsrfResponse(tokens.RequestToken!)));
            })
            .AllowAnonymous()
            .RequireRateLimiting("Identity.Csrf")
            .WithName("Identity.GetCsrf");
    }

    private sealed record CsrfResponse(string RequestToken);
}
