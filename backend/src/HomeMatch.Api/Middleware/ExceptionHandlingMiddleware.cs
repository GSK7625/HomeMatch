using HomeMatch.Shared.Common;

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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client has left; do not turn request cancellation into a JSON 500 response.
            context.Abort();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred. RequestId: {RequestId}", context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                // Headers/body cannot be replaced safely; let the server terminate the response.
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = ApiResponse<string>.Failure(
                new Error("Server.InternalError", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."));

            await context.Response.WriteAsJsonAsync(response, cancellationToken: context.RequestAborted);
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
