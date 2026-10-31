using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace HomeMatch.Modules.Identity.Presentation.Authentication;

public sealed class RefreshCookie(IHostEnvironment environment)
{
    public const string Path = "/api/v1/identity";

    public string Name(HttpContext context) => IsInsecureLocalDevelopment(context) ? "HM.Refresh.Dev" : "__Secure-HM.Refresh";

    public string? Read(HttpContext context)
    {
        var raw = context.Request.Cookies[Name(context)];
        return raw is { Length: 43 } ? raw : null;
    }

    public void Write(HttpContext context, string raw)
    {
        var options = new CookieOptions
        {
            Path = Path, Domain = null, HttpOnly = true, SameSite = SameSiteMode.Lax,
            Secure = !IsInsecureLocalDevelopment(context)
        };
        context.Response.Cookies.Append(Name(context), raw, options);
    }

    public void Delete(HttpContext context)
        => context.Response.Cookies.Append(Name(context), string.Empty, new CookieOptions
        {
            Path = Path, Domain = null, HttpOnly = true, SameSite = SameSiteMode.Lax,
            Secure = !IsInsecureLocalDevelopment(context), Expires = DateTimeOffset.UnixEpoch, MaxAge = TimeSpan.Zero
        });

    private bool IsInsecureLocalDevelopment(HttpContext context) => environment.IsDevelopment() && !context.Request.IsHttps
        && context.Request.Host.Host is "localhost" or "127.0.0.1" or "::1";
}
