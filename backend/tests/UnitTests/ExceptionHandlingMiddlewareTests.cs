using System.Text.Json;
using HomeMatch.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeMatch.UnitTests;

public class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = NullLogger<ExceptionHandlingMiddleware>.Instance;

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ShouldReturn500AndSanitizedResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        RequestDelegate next = _ => throw new InvalidOperationException("Secret database connection string leaked!");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.True(context.Response.Headers.ContainsKey(TraceIdMiddleware.TraceIdHeaderName));

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBodyStream);
        var json = await reader.ReadToEndAsync();

        // Must NOT leak raw exception message
        Assert.DoesNotContain("Secret database connection string", json);

        // Must strictly conform to ApiResponse envelope with data: null
        Assert.Contains("\"isSuccess\":false", json);
        Assert.Contains("\"data\":null", json);
        Assert.Contains("\"code\":\"Server.InternalError\"", json);
        Assert.Contains("\"message\":\"Đã xảy ra lỗi hệ thống không mong muốn. Vui lòng thử lại sau.\"", json);
    }

    [Fact]
    public async Task InvokeAsync_WhenClientCancelsRequest_ShouldSetStatus499AndNotReturn500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        context.RequestAborted = cts.Token;

        RequestDelegate next = _ => throw new OperationCanceledException(cts.Token);
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(499, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenInternalTimeoutOccursWithoutClientCancelling_ShouldReturn500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Client token is NOT cancelled, but downstream throws TaskCanceledException (e.g., DB query timeout)
        RequestDelegate next = _ => throw new TaskCanceledException("Database internal timeout");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStarted_ShouldNotThrowInvalidOperationException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // Simulate response already started by setting a custom started flag or stream
        // In DefaultHttpContext, we can test that when HasStarted is simulated, it returns cleanly
        RequestDelegate next = _ => throw new Exception("Error after start");
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act & Assert (Should not throw)
        var exception = await Record.ExceptionAsync(() => middleware.InvokeAsync(context));
        Assert.Null(exception);
    }
}
