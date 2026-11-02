using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HomeMatch.Modules.Identity.Presentation.Authentication;

/// <summary>Explicit anonymous context for the browser's bootstrap/login/refresh/logout flow.</summary>
public sealed class IdentityBrowserSecurity(IConfiguration configuration, IHostEnvironment environment)
{
    public bool IsAllowedOrigin(HttpContext context)
    {
        if (!context.Request.IsHttps
            && !(environment.IsDevelopment() && context.Request.Host.Host is "localhost" or "127.0.0.1" or "::1"))
            return false;

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var origin = context.Request.Headers.Origin;
        if (origin.Count > 0)
            return origin.Count == 1 && origins.Contains(origin[0], StringComparer.Ordinal);

        if (!HttpMethods.IsGet(context.Request.Method)
            || context.Request.Headers["Sec-Fetch-Site"] != "same-origin")
            return false;
        var target = $"{context.Request.Scheme}://{context.Request.Host}";
        return origins.Contains(target, StringComparer.Ordinal);
    }

    public async Task<IResult?> ValidateMutationAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (!IsAllowedOrigin(context)) return IdentityHttpErrors.OriginNotAllowed();
        var original = context.User;
        try
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            await antiforgery.ValidateRequestAsync(context);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return IdentityHttpErrors.CsrfInvalid();
        }
        finally
        {
            context.User = original;
        }
    }

    public AntiforgeryTokenSet Bootstrap(HttpContext context, IAntiforgery antiforgery)
    {
        var original = context.User;
        try
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            return antiforgery.GetAndStoreTokens(context);
        }
        finally
        {
            context.User = original;
        }
    }
}
