using System.Globalization;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Logout;
using HomeMatch.Modules.Identity.Presentation.Authentication;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Logout;

public static class LogoutEndpoints
{
    public static async Task<IResult> CurrentAsync(HttpContext context, IdentityBrowserSecurity browser,
        IAntiforgery antiforgery, LogoutRequestReader reader, LogoutRateLimiter limiter,
        ILogoutSessionService service, RefreshCookie cookie)
    {
        var failure = await browser.ValidateMutationAsync(context, antiforgery);
        if (failure is not null) return failure;
        var retry = limiter.TryCurrent(ClientAddress(context));
        if (retry is not null) return RateLimited(context, retry.Value);
        var body = await reader.ReadAsync(context.Request, context.RequestAborted);
        if (body.Error != IdentityJsonError.None) return BodyFailure(body.Error);
        var result = await service.LogoutCurrentAsync(cookie.Read(context), context.RequestAborted);
        if (result.Outcome == LogoutOutcome.Unavailable) return IdentityHttpErrors.DependencyUnavailable();
        cookie.Delete(context);
        return Results.NoContent();
    }

    public static async Task<IResult> AllAsync(HttpContext context, ICurrentIdentity current, LogoutRequestReader reader,
        LogoutRateLimiter limiter, ILogoutSessionService service, RefreshCookie cookie)
    {
        if (current.Identity is not { } identity) return AuthenticationRequired(context);
        var retry = limiter.TryAll(ClientAddress(context), identity.UserId);
        if (retry is not null) return RateLimited(context, retry.Value);
        var body = await reader.ReadAsync(context.Request, context.RequestAborted);
        if (body.Error != IdentityJsonError.None) return BodyFailure(body.Error);
        var result = await service.LogoutAllAsync(new LogoutAllSubject(identity.UserId, identity.SessionId, identity.Role),
            cookie.Read(context), context.RequestAborted);
        if (result.Outcome == LogoutOutcome.Unavailable) return IdentityHttpErrors.DependencyUnavailable();
        if (result.Outcome == LogoutOutcome.InvalidAuthentication) return AuthenticationRequired(context);
        if (result.ClearRefreshCookie) cookie.Delete(context);
        return Results.NoContent();
    }

    private static string ClientAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static IResult RateLimited(HttpContext context, TimeSpan retry)
    {
        context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        return IdentityHttpErrors.RateLimited();
    }

    private static IResult BodyFailure(IdentityJsonError error) => error switch
    {
        IdentityJsonError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
        IdentityJsonError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
        _ => IdentityHttpErrors.ValidationFailed()
    };

    private static IResult AuthenticationRequired(HttpContext context)
    {
        context.Response.Headers.WWWAuthenticate = "Bearer";
        return IdentityHttpErrors.AuthenticationRequired();
    }
}
