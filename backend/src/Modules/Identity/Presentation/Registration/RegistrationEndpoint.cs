using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.DTOs;
using HomeMatch.Modules.Identity.Application.PublicOtp;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Modules.Identity.Presentation.Http;
using HomeMatch.Modules.Identity.Presentation.PublicOtp;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace HomeMatch.Modules.Identity.Presentation.Registration;

public static class RegistrationEndpoint
{
    public static async Task<IResult> HandleAsync(HttpContext context,
        IdentityQuotas limiter, AuthService service, SendPublicOtpHandler otp,
        IdentityBody<RegistrationRequestDto> body)
    {
        var normalized = AccountInput.TryNormalizeEmail(body.Value?.Email, out _, out var emailKey) ? emailKey : null;
        var retry = limiter.TryRegistration(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", normalized);
        if (retry is not null)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.Value.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            return IdentityHttpErrors.RateLimited();
        }
        if (body.Error != IdentityJsonError.None) return body.Error switch
        {
            IdentityJsonError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
            IdentityJsonError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
            _ => IdentityHttpErrors.ValidationFailed()
        };
        var outcome = await service.RegisterAsync(body.Value!, context.RequestAborted);
        if (outcome == RegistrationOutcome.Invalid) return IdentityHttpErrors.ValidationFailed();
        if (outcome != RegistrationOutcome.Accepted) return IdentityHttpErrors.DependencyUnavailable();
        var result = await otp.HandleAsync(normalized!, "Registration", body.Value!.Password, context.RequestAborted);
        if (result.IsSuccess)
            return Results.Json(ApiResponse<PublicOtpResponse>.Success(result.Value),
                statusCode: StatusCodes.Status202Accepted);
        return PublicOtpEndpoints.MapResult(context, result);
    }
}
