using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth;

public static class CookieAuthHelper
{
    public const string ProdCookieName = "__Secure-HM.Refresh";
    public const string DevCookieName = "HM.Refresh.Dev";
    public const string CookiePath = "/api/v1/identity";

    public static string GetCookieName(HttpContext context)
    {
        return context.Request.IsHttps ? ProdCookieName : DevCookieName;
    }

    public static string? GetRefreshTokenFromCookie(HttpContext context)
    {
        var cookieName = GetCookieName(context);
        if (context.Request.Cookies.TryGetValue(cookieName, out var cookieValue) && !string.IsNullOrWhiteSpace(cookieValue))
        {
            return cookieValue;
        }

        // Dự phòng kiểm tra cả hai tên cookie
        if (context.Request.Cookies.TryGetValue(ProdCookieName, out var prodVal) && !string.IsNullOrWhiteSpace(prodVal))
        {
            return prodVal;
        }

        if (context.Request.Cookies.TryGetValue(DevCookieName, out var devVal) && !string.IsNullOrWhiteSpace(devVal))
        {
            return devVal;
        }

        return null;
    }

    public static void SetRefreshTokenCookie(HttpContext context, string refreshToken, DateTimeOffset expiresAt)
    {
        var cookieName = GetCookieName(context);
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Path = CookiePath,
            Expires = expiresAt
        };

        context.Response.Cookies.Append(cookieName, refreshToken, options);
    }

    public static void ClearRefreshTokenCookie(HttpContext context)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Path = CookiePath,
            Expires = DateTimeOffset.UnixEpoch
        };

        context.Response.Cookies.Append(ProdCookieName, string.Empty, options);
        context.Response.Cookies.Append(DevCookieName, string.Empty, options);
    }
}
