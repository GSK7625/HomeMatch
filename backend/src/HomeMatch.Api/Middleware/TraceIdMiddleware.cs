using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Api.Middleware;

public class TraceIdMiddleware
{
    public const string TraceIdHeaderName = "X-Trace-Id";
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string traceId;
        if (context.Request.Headers.TryGetValue(TraceIdHeaderName, out var incomingTraceId)
            && !string.IsNullOrWhiteSpace(incomingTraceId))
        {
            traceId = incomingTraceId.ToString();
        }
        else
        {
            traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        }

        context.Items[TraceIdHeaderName] = traceId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(TraceIdHeaderName))
            {
                context.Response.Headers.Append(TraceIdHeaderName, traceId);
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class TraceIdMiddlewareExtensions
{
    public static IApplicationBuilder UseTraceId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TraceIdMiddleware>();
    }
}
