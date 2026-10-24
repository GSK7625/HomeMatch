using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Leasing;

public static class LeasingModuleExtensions
{
    public static IServiceCollection AddLeasingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register Leasing services, repositories, validators, and DbContext
        return services;
    }

    public static IEndpointRouteBuilder MapLeasingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/leasing").WithTags("Leasing");
        group.MapGet("/ping", () => Results.Ok(new { module = "Leasing", status = "Healthy", timestamp = DateTime.UtcNow }));
        return endpoints;
    }
}
