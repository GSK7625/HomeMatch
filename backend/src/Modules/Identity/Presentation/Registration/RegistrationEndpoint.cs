using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.PublicOtp;
using HomeMatch.Modules.Identity.Presentation.PublicOtp;
using System.Globalization;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Registration;

public static class RegistrationEndpoint
{
    public static async Task<IResult> HandleAsync(HttpContext context, RegistrationRequestReader reader,
        RegistrationRateLimiter limiter, AuthService service, SendPublicOtpHandler otp)
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
        var outcome = await service.RegisterAsync(read.Command!, context.RequestAborted);
        if (outcome == RegistrationOutcome.Invalid) return IdentityHttpErrors.ValidationFailed();
        if (outcome != RegistrationOutcome.Accepted) return IdentityHttpErrors.DependencyUnavailable();
        var result = await otp.HandleAsync(normalized!, "Registration", read.Command!.Password, context.RequestAborted);
        if (result.IsSuccess)
            return Results.Json(ApiResponse<PublicOtpResponse>.Success(result.Value),
                statusCode: StatusCodes.Status202Accepted);
        return PublicOtpEndpoints.MapResult(context, result);
    }
}
