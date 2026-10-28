using System.Data.Common;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.Authentication;

public sealed class IdentityJwtEvents(IAccessIdentityReader reader, IOptions<JwtOptions> options,
    ILogger<IdentityJwtEvents> logger) : JwtBearerEvents
{
    public override Task MessageReceived(MessageReceivedContext context)
    {
        // Endpoint policies decide whether Bearer authentication is needed, regardless of URL spelling.
        // Public routes still ignore stale/foreign Authorization headers.
        if (context.HttpContext.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>().Count is not > 0)
            context.NoResult();
        return Task.CompletedTask;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        if (context.Principal is null
            || !AccessTokenClaims.TryRead(context.Principal, options.Value.ExpirationMinutes, out var identity))
        {
            context.Fail("Invalid access token claims.");
            return;
        }

        try
        {
            var current = await reader.FindActiveAsync(identity!, context.HttpContext.RequestAborted);
            if (current is null) context.Fail("Invalid account or session.");
            else context.HttpContext.Features.Set(current);
        }
        catch (Exception exception) when (exception is DbException or TimeoutException
            or InvalidOperationException { InnerException: DbException })
        {
            // Do not turn an unavailable store into a successful authentication or leak DB details.
            logger.LogWarning("Identity authentication store is unavailable.");
            context.HttpContext.Features.Set(new AuthenticationStoreUnavailable());
            context.Fail("Authentication store unavailable.");
        }
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.Headers.CacheControl = "no-store";
        if (context.HttpContext.Features.Get<AuthenticationStoreUnavailable>() is not null)
            await IdentityHttpErrors.DependencyUnavailable().ExecuteAsync(context.HttpContext);
        else
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await IdentityHttpErrors.AuthenticationRequired().ExecuteAsync(context.HttpContext);
        }
    }

    public override Task Forbidden(ForbiddenContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        return IdentityHttpErrors.Forbidden().ExecuteAsync(context.HttpContext);
    }

    private sealed class AuthenticationStoreUnavailable;
}
