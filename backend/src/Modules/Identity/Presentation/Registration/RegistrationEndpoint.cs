using System.Globalization;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain.Validation;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Registration;

public static class RegistrationEndpoint
{
    public const string AcceptedMessage = "Yêu cầu đăng ký đã được xử lý. Bạn có thể thử đăng nhập bằng thông tin đã cung cấp.";

    public static async Task<IResult> HandleAsync(HttpContext context, RegistrationRequestReader reader,
        RegistrationRateLimiter limiter, RegisterAccountHandler handler)
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
        return await handler.HandleAsync(read.Command!, context.RequestAborted) switch
        {
            RegistrationOutcome.Accepted => Results.Ok(ApiResponse<RegistrationResponse>.Success(new(AcceptedMessage))),
            RegistrationOutcome.Invalid => IdentityHttpErrors.ValidationFailed(),
            _ => IdentityHttpErrors.DependencyUnavailable()
        };
    }

    public sealed record RegistrationResponse(string Message);
}
