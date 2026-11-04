using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Presentation.Security;

public static class RateLimitingSetup
{
    public const string RegisterPolicy = "auth-register";
    public const string LoginPolicy = "auth-login";
    public const string ForgotPasswordPolicy = "auth-forgot";

    public static IServiceCollection AddIdentityRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var error = new Error(
                    IdentityErrors.RateLimitExceeded,
                    "Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau ít phút.");

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse.Failure<object>(error),
                    cancellationToken: token);
            };

            // 1. Policy Đăng ký: Tối đa 5 yêu cầu / 15 phút theo IP
            options.AddPolicy(RegisterPolicy, httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // 2. Policy Đăng nhập: Tối đa 10 yêu cầu / 1 phút theo IP
            options.AddPolicy(LoginPolicy, httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // 3. Policy Quên mật khẩu: Tối đa 5 yêu cầu / 15 phút theo IP
            options.AddPolicy(ForgotPasswordPolicy, httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}
