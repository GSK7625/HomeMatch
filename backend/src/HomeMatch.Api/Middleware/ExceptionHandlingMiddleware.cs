using System.Net;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeMatch.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is not ApiException)
                _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var apiException = exception as ApiException;
        if (apiException is null && exception is DbUpdateException
            { InnerException: PostgresException postgres })
        {
            apiException = postgres.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ApiException.Conflict(
                    "Database.Duplicate", "Dữ liệu đã tồn tại."),
                PostgresErrorCodes.ForeignKeyViolation => ApiException.BadRequest(
                    "Database.InvalidReference", "Dữ liệu tham chiếu không hợp lệ."),
                _ => null
            };
        }
        context.Response.StatusCode = apiException?.StatusCode ?? (int)HttpStatusCode.InternalServerError;
        var response = ApiResponse<string>.Failure(new Error(
            apiException?.Code ?? "Server.InternalError",
            apiException?.Message ?? "An unexpected server error occurred."));
        return context.Response.WriteAsJsonAsync(response);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
