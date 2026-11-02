using HomeMatch.Modules.Identity.Application.Authentication;
using System.Globalization;
using HomeMatch.Modules.Identity.Application.PublicOtp;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Presentation.Http;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace HomeMatch.Modules.Identity.Presentation.PublicOtp;
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
            (HttpContext c, SendPublicOtpHandler send, VerifyPublicOtpHandler verify, ResetPasswordHandler reset,
                IdentityQuotas limiter, IdentityBody<PublicOtpBody> body)
                => HandleAsync(c, send, verify, reset, limiter, body, purpose, action)).AllowAnonymous();
    }
    public sealed class PublicOtpBody
    {
        public string? Email { get; init; }
        public string? Password { get; init; }
        public string? ChallengeId { get; init; }
        public string? Code { get; init; }
        public string? ResetToken { get; init; }
        public string? NewPassword { get; init; }
        public override string ToString() => "PublicOtpBody { [redacted] }";
    }
    private sealed record Input(string Email, string Password, Guid Id, string Code, string Token)
    {
        public override string ToString() => "Input { [redacted] }";
    }
    private static async Task<IResult> HandleAsync(HttpContext c, SendPublicOtpHandler send, VerifyPublicOtpHandler verify,
        ResetPasswordHandler reset, IdentityQuotas limiter, IdentityBody<PublicOtpBody> body,
        string purpose, string action)
    {
        var input = body.Error == IdentityJsonError.None ? Parse(body.Value!, body.PropertyCount, purpose, action) : null;
        var retry = limiter.TryPublicOtp(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", action == "send" ? input?.Email : null, action == "send");
        if (retry is not null) { c.Response.Headers.RetryAfter = Math.Ceiling(retry.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture); return IdentityHttpErrors.RateLimited(); }
        if (body.Error != IdentityJsonError.None) return body.Error switch
        {
            IdentityJsonError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
            IdentityJsonError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
            _ => IdentityHttpErrors.ValidationFailed()
        };
        if (input is null) return IdentityHttpErrors.ValidationFailed();
        var result = action == "send" ? await send.HandleAsync(input!.Email, purpose, input.Password, c.RequestAborted)
            : action == "verify" ? await verify.HandleAsync(input!.Id, input.Code, purpose, c.RequestAborted)
            : await reset.HandleAsync(input!.Token, input.Password, c.RequestAborted);
        return MapResult(c, result);
    }
    private static Input? Parse(PublicOtpBody body, int count, string purpose, string action)
    {
        if (action == "send")
        {
            if (count != (purpose == "Registration" ? 2 : 1)
                || !AccountInput.TryNormalizeEmail(body.Email, out _, out var email)) return null;
            var password = body.Password ?? "";
            if (purpose == "Registration" && (body.Password is null || password.Length is < 1 or > 256)) return null;
            return new(email, password, Guid.Empty, "", "");
        }
        if (action == "verify")
        {
            if (count != 2 || !Guid.TryParseExact(body.ChallengeId, "D", out var id) || id == Guid.Empty
                || body.Code is not { Length: 6 } code || !code.All(char.IsAsciiDigit)) return null;
            return new("", "", id, code, "");
        }
        return count == 2 && body.ResetToken is not null && body.NewPassword is not null
            ? new("", body.NewPassword, Guid.Empty, "", body.ResetToken) : null;
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
