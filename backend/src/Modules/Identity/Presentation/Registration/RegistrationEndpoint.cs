using System.Globalization;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Registration;

public static class RegistrationEndpoint
{
    public const string AcceptedMessage = "Vui lòng kiểm tra email và xác thực OTP để hoàn tất đăng ký.";

    public static async Task<IResult> HandleAsync(HttpContext context, RegistrationRequestReader reader,
        RegistrationRateLimiter limiter, RegisterAccountHandler handler, HomeMatch.Modules.Identity.Application.PublicOtp.IPublicOtpService otp)
    {
        var read = await reader.ReadAsync(context.Request, context.RequestAborted);
        var normalized = AccountInput.TryNormalizeEmail(read.Command?.Email, out _, out var emailKey) ? emailKey : null;
        var retry = limiter.TryAcquire(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", normalized);
        if (retry is not null)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.Value.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            return IdentityHttpErrors.RateLimited();
        }
        if (read.Error != RegistrationReadError.None) return read.Error switch
        {
            RegistrationReadError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
            RegistrationReadError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
            _ => IdentityHttpErrors.ValidationFailed()
        };
        var outcome = await handler.HandleAsync(read.Command!, context.RequestAborted);
        if (outcome == RegistrationOutcome.Invalid) return IdentityHttpErrors.ValidationFailed();
        if (outcome != RegistrationOutcome.Accepted) return IdentityHttpErrors.DependencyUnavailable();
        try
        {
            var result = await otp.SendAsync(normalized!, "Registration", read.Command!.Password, context.RequestAborted);
            if (result.IsSuccess)
                return Results.Json(ApiResponse<HomeMatch.Modules.Identity.Application.PublicOtp.PublicOtpResponse>.Success(result.Value),
                    statusCode: StatusCodes.Status202Accepted);
            return HomeMatch.Modules.Identity.Presentation.PublicOtp.PublicOtpEndpoints.MapResult(context, result);
        }
        catch (Exception e) when (HomeMatch.Modules.Identity.Infrastructure.Persistence.IdentityDatabaseFailure.IsUnavailable(e))
        { return IdentityHttpErrors.DependencyUnavailable(); }
    }

    public sealed record RegistrationResponse(string Message);
}
