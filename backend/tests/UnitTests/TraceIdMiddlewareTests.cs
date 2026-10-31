using HomeMatch.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.UnitTests;

public class TraceIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestHasNoTraceId_ShouldGenerateTraceIdInItems()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = ctx => Task.CompletedTask;
        var middleware = new TraceIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Items.ContainsKey(TraceIdMiddleware.TraceIdHeaderName));
        var traceId = context.Items[TraceIdMiddleware.TraceIdHeaderName]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(traceId));
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasTraceId_ShouldPreserveSameTraceId()
    {
        // Arrange
        const string customTraceId = "my-custom-trace-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers[TraceIdMiddleware.TraceIdHeaderName] = customTraceId;

        RequestDelegate next = ctx => Task.CompletedTask;
        var middleware = new TraceIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(customTraceId, context.Items[TraceIdMiddleware.TraceIdHeaderName]);
    }
}
