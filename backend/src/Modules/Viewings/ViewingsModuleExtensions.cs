using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Viewings;

public static class ViewingsModuleExtensions
{
    public static IServiceCollection AddViewingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register Viewings services, repositories, validators, and DbContext
        return services;
    }

    public static IEndpointRouteBuilder MapViewingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/viewings").WithTags("Viewings");
        group.MapGet("/ping", () => Results.Ok(new { module = "Viewings", status = "Healthy", timestamp = DateTime.UtcNow }));
        return endpoints;
    }
}
