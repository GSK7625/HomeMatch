using System.Globalization;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Presentation.Authentication;
using HomeMatch.Modules.Identity.Presentation.Http;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Login;

public static class LoginEndpoint
{
    public static async Task<IResult> HandleAsync(HttpContext context, IdentityBrowserSecurity browser,
        IAntiforgery antiforgery, LoginRequestReader reader, LoginRateLimiter limiter, LoginHandler handler, RefreshCookie cookie)
    {
        var failure = await browser.ValidateMutationAsync(context, antiforgery);
        if (failure is not null) return failure;
        var read = await reader.ReadAsync(context.Request, context.RequestAborted);
        var email = AccountInput.TryNormalizeEmail(read.Value?.Email, out _, out var normalized) ? normalized : null;
        var retry = limiter.TryAcquire(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", email);
        if (retry is not null)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.Value.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            return IdentityHttpErrors.RateLimited();
        }
        if (read.Error != IdentityJsonError.None) return read.Error switch
        {
            IdentityJsonError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
            IdentityJsonError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
            _ => IdentityHttpErrors.ValidationFailed()
        };

        var result = await handler.HandleAsync(read.Value!, cookie.Read(context), context.RequestAborted);
        if (result.Outcome == LoginOutcome.Succeeded)
        {
            cookie.Write(context, result.RefreshSecret!, result.RememberMe, result.Token!.SessionExpiresAt);
            return Results.Ok(ApiResponse<IssuedAccessToken>.Success(result.Token));
        }
        if (result.Outcome == LoginOutcome.InvalidCredentials) context.Response.Headers.WWWAuthenticate = "Bearer";
        return result.Outcome switch
        {
            LoginOutcome.InvalidInput => IdentityHttpErrors.ValidationFailed(),
            LoginOutcome.InvalidCredentials => IdentityHttpErrors.InvalidCredentials(),
            LoginOutcome.AccountSuspended => IdentityHttpErrors.AccountSuspended(),
            _ => IdentityHttpErrors.DependencyUnavailable()
        };
    }
}
