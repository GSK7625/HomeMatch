using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Listings;

public static class ListingsModuleExtensions
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register Listings services, repositories, validators, and DbContext
        return services;
    }

    public static IEndpointRouteBuilder MapListingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/listings").WithTags("Listings");
        group.MapGet("/ping", () => Results.Ok(new { module = "Listings", status = "Healthy", timestamp = DateTime.UtcNow }));
        return endpoints;
    }
}
