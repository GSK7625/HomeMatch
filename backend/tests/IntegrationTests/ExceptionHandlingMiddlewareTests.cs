using HomeMatch.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeMatch.IntegrationTests;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task StartedResponseIsNotRewrittenWhenAnExceptionOccurs()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        var failure = new InvalidOperationException("streaming failure");
        var middleware = new ExceptionHandlingMiddleware(_ => throw failure, NullLogger<ExceptionHandlingMiddleware>.Instance);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Same(failure, thrown);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task ClientCancellationAbortsWithoutWritingAnErrorEnvelope()
    {
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;
        var lifetime = new CancelledRequestLifetime();
        context.Features.Set<IHttpRequestLifetimeFeature>(lifetime);
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(lifetime.RequestAborted),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(lifetime.Aborted);
        Assert.Equal(0, body.Length);
        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private sealed class CancelledRequestLifetime : IHttpRequestLifetimeFeature
    {
        public CancellationToken RequestAborted { get; set; } = new(true);
        public bool Aborted { get; private set; }
        public void Abort() => Aborted = true;
    }
}
