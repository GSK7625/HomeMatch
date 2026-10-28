using System.Globalization;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Refresh;
using HomeMatch.Modules.Identity.Presentation.Authentication;
using HomeMatch.Modules.Identity.Presentation.Http;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Refresh;

public static class RefreshEndpoint
{
    public static async Task<IResult> HandleAsync(HttpContext context, IdentityBrowserSecurity browser,
        IAntiforgery antiforgery, RefreshRequestReader reader, IRefreshSessionService service,
        IRefreshRequestQuota quota, RefreshCookie cookie)
    {
        var failure = await browser.ValidateMutationAsync(context, antiforgery);
        if (failure is not null) return failure;
        var read = await reader.ReadAsync(context.Request, context.RequestAborted);
        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (read.Error != IdentityJsonError.None)
        {
            var retry = quota.TryAcquire(client, null);
            if (retry is not null) return RateLimited(context, retry.Value);
            return read.Error switch
            {
                IdentityJsonError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
                IdentityJsonError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
                _ => IdentityHttpErrors.ValidationFailed()
            };
        }

        var result = await service.RefreshAsync(cookie.Read(context), client, context.RequestAborted);
        if (result.Outcome == RefreshOutcome.Succeeded)
        {
            cookie.Write(context, result.RefreshSecret!, result.RememberMe, result.RefreshExpiresAt!.Value);
            return Results.Ok(ApiResponse<IssuedAccessToken>.Success(result.Token!));
        }
        if (result.Outcome == RefreshOutcome.RateLimited) return RateLimited(context, result.RetryAfter!.Value);
        if (result.Outcome == RefreshOutcome.Unavailable) return IdentityHttpErrors.DependencyUnavailable();
        cookie.Delete(context);
        context.Response.Headers.WWWAuthenticate = "Bearer";
        return IdentityHttpErrors.SessionInvalid();
    }

    private static IResult RateLimited(HttpContext context, TimeSpan retry)
    {
        context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        return IdentityHttpErrors.RateLimited();
    }
}
