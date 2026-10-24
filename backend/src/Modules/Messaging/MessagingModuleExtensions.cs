using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Messaging;

public static class MessagingModuleExtensions
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register Messaging services, repositories, validators, and DbContext
        return services;
    }

    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/messaging").WithTags("Messaging");
        group.MapGet("/ping", () => Results.Ok(new { module = "Messaging", status = "Healthy", timestamp = DateTime.UtcNow }));
        return endpoints;
    }
}
