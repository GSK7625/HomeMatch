using System.Diagnostics;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        // Intentionally do NOT set JsonIgnoreCondition.WhenWritingNull
        // so that "data": null is explicitly output in the response JSON contract.
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
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            var traceId = GetTraceId(context);
            _logger.LogInformation(ex, "[{TraceId}] Request was cancelled by the client.", traceId);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499; // Client Closed Request
            }
        }
        catch (Exception ex)
        {
            var traceId = GetTraceId(context);
            _logger.LogError(ex, "[{TraceId}] An unhandled exception occurred: {Message}", traceId, ex.Message);
            await HandleExceptionAsync(context, traceId);
        }
    }

    private static string GetTraceId(HttpContext context)
    {
        if (context.Items.TryGetValue(TraceIdMiddleware.TraceIdHeaderName, out var itemTraceId) && itemTraceId is string idStr)
        {
            return idStr;
        }

        return Activity.Current?.Id ?? context.TraceIdentifier;
    }

    private static Task HandleExceptionAsync(HttpContext context, string traceId)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        if (!context.Response.Headers.ContainsKey(TraceIdMiddleware.TraceIdHeaderName))
        {
            context.Response.Headers.Append(TraceIdMiddleware.TraceIdHeaderName, traceId);
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
