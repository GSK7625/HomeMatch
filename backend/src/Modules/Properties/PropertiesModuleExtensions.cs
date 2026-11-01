using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Properties;

public static class PropertiesModuleExtensions
{
    public static IServiceCollection AddPropertiesModule(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register Properties services, repositories, validators, and DbContext
        return services;
    }

    public static IEndpointRouteBuilder MapPropertiesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/properties").WithTags("Properties");
        group.MapGet("/ping", () => Results.Ok(new { module = "Properties", status = "Healthy", timestamp = DateTime.UtcNow }));
        return endpoints;
    }
}
