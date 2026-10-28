using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;

namespace HomeMatch.Modules.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity")));
        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity");
        group.MapGet("/ping", () => Results.Ok(new { module = "Identity", status = "Healthy", timestamp = DateTime.UtcNow }));
        return endpoints;
    }
}
