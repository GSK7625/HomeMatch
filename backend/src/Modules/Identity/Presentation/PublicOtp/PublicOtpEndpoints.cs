using HomeMatch.Modules.Identity.Application.Authentication;
using System.Globalization;
using System.Text.Json;
using HomeMatch.Modules.Identity.Application.PublicOtp;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Presentation.Http;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
namespace HomeMatch.Modules.Identity.Presentation.PublicOtp;
public sealed class PublicOtpLimiter(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly AtomicRequestQuota _send = new(options, clock, "public-otp-send", 30, TimeSpan.FromMinutes(1), 5, TimeSpan.FromMinutes(15));
    private readonly AtomicRequestQuota _verify = new(options, clock, "public-otp-verify", 60, TimeSpan.FromMinutes(1), 20, TimeSpan.FromMinutes(15));
    public TimeSpan? Acquire(string ip, string? subject, bool sending) => (sending ? _send : _verify).TryAcquire(ip, subject);
}
public static class PublicOtpEndpoints
{
    public static void MapPublicOtp(this RouteGroupBuilder group)
    {
        Map("/auth/registration/send-otp", "Registration", "send");
        Map("/auth/registration/verify-otp", "Registration", "verify");
        Map("/auth/forgot-password", "PasswordReset", "send");
        Map("/auth/forgot-password/verify-otp", "PasswordReset", "verify");
        Map("/auth/reset-password", "PasswordReset", "reset");
        void Map(string route, string purpose, string action) => group.MapPost(route,
            (HttpContext c, SendPublicOtpHandler send, VerifyPublicOtpHandler verify, ResetPasswordHandler reset, PublicOtpLimiter limiter)
                => HandleAsync(c, send, verify, reset, limiter, purpose, action)).AllowAnonymous();
    }
    private sealed record Input(string Email, string Password, Guid Id, string Code, string Token)
    {
        public override string ToString() => "Input { [redacted] }";
    }
    private static async Task<IResult> HandleAsync(HttpContext c, SendPublicOtpHandler send, VerifyPublicOtpHandler verify,
        ResetPasswordHandler reset, PublicOtpLimiter limiter, string purpose, string action)
    {
        var read = await IdentityJsonReader.ReadAsync<Input>(c.Request, root =>
        {
            string? Get(string key) => root.TryGetProperty(key, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() : null;
            var fields = action == "send" ? (purpose == "Registration" ? new[] { "email", "password" } : new[] { "email" })
                : action == "verify" ? new[] { "challengeId", "code" } : new[] { "resetToken", "newPassword" };
            if (root.EnumerateObject().Count() != fields.Length || fields.Any(x => Get(x) is null)) return null;
            if (action == "send")
            {
                if (!AccountInput.TryNormalizeEmail(Get("email"), out _, out var email)) return null;
                var password = Get("password") ?? "";
                if (purpose == "Registration" && (password.Length is < 1 or > 256)) return null;
                return new(email, password, Guid.Empty, "", "");
            }
            if (action == "verify")
            {
                if (!Guid.TryParseExact(Get("challengeId"), "D", out var id) || id == Guid.Empty
                    || Get("code") is not { Length: 6 } code || !code.All(char.IsAsciiDigit)) return null;
                return new("", "", id, code, "");
            }
            return new("", Get("newPassword")!, Guid.Empty, "", Get("resetToken")!);
        }, c.RequestAborted);
        var input = read.Value;
        var retry = limiter.Acquire(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", action == "send" ? input?.Email : null, action == "send");
        if (retry is not null) { c.Response.Headers.RetryAfter = Math.Ceiling(retry.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture); return IdentityHttpErrors.RateLimited(); }
        if (read.Error != IdentityJsonError.None) return read.Error switch
        {
            IdentityJsonError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
            IdentityJsonError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
            _ => IdentityHttpErrors.ValidationFailed()
        };
        var result = action == "send" ? await send.HandleAsync(input!.Email, purpose, input.Password, c.RequestAborted)
            : action == "verify" ? await verify.HandleAsync(input!.Id, input.Code, purpose, c.RequestAborted)
            : await reset.HandleAsync(input!.Token, input.Password, c.RequestAborted);
        return MapResult(c, result);
    }
    public static IResult MapResult(HttpContext context, Result<PublicOtpResponse> result)
    {
        if (result.IsSuccess) return Results.Ok(ApiResponse<PublicOtpResponse>.Success(result.Value));
        if (result.Error is PublicOtpRateLimitError rateLimit)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, rateLimit.RetryAfterSeconds).ToString(CultureInfo.InvariantCulture);
            return IdentityHttpErrors.RateLimited();
        }
        var status = result.Error == PublicOtpErrors.Unavailable ? 503 : 400;
        return Results.Json(ApiResponse<object>.Failure(result.Error), statusCode: status);
    }
}
