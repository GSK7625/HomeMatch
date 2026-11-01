using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;
using HomeMatch.Modules.Identity.Presentation.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.IntegrationTests.Infrastructure;

/// <summary>Test-only terminal delegate; it runs inside the application's middleware pipeline.</summary>
internal sealed class ProbeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/__tests/admin", () => Results.NoContent())
                .RequireAuthorization(IdentityPolicies.Admin);
            endpoints.MapPost("/api/v1/identity/auth/__tests/csrf", async context =>
            {
                var security = context.RequestServices.GetRequiredService<IdentityBrowserSecurity>();
                var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                var failure = await security.ValidateMutationAsync(context, antiforgery);
                if (failure is not null) await failure.ExecuteAsync(context);
                else context.Response.StatusCode = context.User.Identity?.IsAuthenticated == true
                    ? StatusCodes.Status500InternalServerError : StatusCodes.Status204NoContent;
            }).AllowAnonymous();
        });
        app.Run(context =>
        {
            if (context.Request.Path == "/__tests/throw")
            {
                throw new InvalidOperationException("private-database-detail-for-test-only");
            }

            context.Response.StatusCode = context.Request.Path == "/__tests/no-content"
                ? StatusCodes.Status204NoContent
                : StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
    };
}
