using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private const string TraceIdHeaderName = "X-Trace-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            _logger.LogError(ex, "[{TraceId}] An unhandled exception occurred: {Message}", traceId, ex.Message);
            await HandleExceptionAsync(context, traceId);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, string traceId)
    {
        if (!context.Response.Headers.ContainsKey(TraceIdHeaderName))
        {
            context.Response.Headers.Append(TraceIdHeaderName, traceId);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var error = new Error("Server.InternalError", "Đã xảy ra lỗi hệ thống không mong muốn. Vui lòng thử lại sau.");
        var response = ApiResponse<object>.Failure(error);
        var json = JsonSerializer.Serialize(response, JsonOptions);

        return context.Response.WriteAsync(json);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
