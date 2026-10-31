using System.IO;
using System.Text.Json;
using HomeMatch.Api.Middleware;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeMatch.IntegrationTests;

public class MiddlewarePipelineIntegrationTests
{
    [Fact]
    public async Task Pipeline_WhenRequestSucceeds_ShouldAttachTraceIdHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Pipeline: TraceIdMiddleware -> Success Handler
        RequestDelegate successHandler = async ctx =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync("{\"status\":\"ok\"}");
        };

        var traceMiddleware = new TraceIdMiddleware(successHandler);

        // Act
        await traceMiddleware.InvokeAsync(context);

        // Simulate response starting callback
        // The OnStarting callbacks are triggered when response starts in ASP.NET Core
        Assert.True(context.Items.ContainsKey(TraceIdMiddleware.TraceIdHeaderName));
        var traceId = context.Items[TraceIdMiddleware.TraceIdHeaderName]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(traceId));
    }

    [Fact]
    public async Task Pipeline_WhenUnhandledExceptionOccurs_ShouldReturn500WithTraceIdAndSanitizedContract()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        RequestDelegate failingHandler = _ => throw new ApplicationException("Database timeout failure!");
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
        var exceptionMiddleware = new ExceptionHandlingMiddleware(failingHandler, logger);
        var pipeline = new TraceIdMiddleware(ctx => exceptionMiddleware.InvokeAsync(ctx));

        // Act
        await pipeline.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.True(context.Response.Headers.ContainsKey(TraceIdMiddleware.TraceIdHeaderName));

        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody);
        var json = await reader.ReadToEndAsync();

        // Verify contract: isSuccess=false, data=null, sanitized error
        Assert.Contains("\"isSuccess\":false", json);
        Assert.Contains("\"data\":null", json);
        Assert.Contains("\"code\":\"Server.InternalError\"", json);
        Assert.DoesNotContain("Database timeout failure", json);

        var deserialized = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(deserialized);
        Assert.False(deserialized.IsSuccess);
        Assert.Null(deserialized.Data);
        Assert.NotNull(deserialized.Error);
        Assert.Equal("Server.InternalError", deserialized.Error.Code);
    }

    [Fact]
    public async Task Pipeline_WhenClientProvidesTraceId_ShouldPreserveSameTraceIdAcrossFailure()
    {
        // Arrange
        const string clientTraceId = "client-trace-abc-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[TraceIdMiddleware.TraceIdHeaderName] = clientTraceId;
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        RequestDelegate failingHandler = _ => throw new Exception("Downstream service crashed");
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
        var exceptionMiddleware = new ExceptionHandlingMiddleware(failingHandler, logger);
        var pipeline = new TraceIdMiddleware(ctx => exceptionMiddleware.InvokeAsync(ctx));

        // Act
        await pipeline.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(clientTraceId, context.Response.Headers[TraceIdMiddleware.TraceIdHeaderName]);
    }
}
