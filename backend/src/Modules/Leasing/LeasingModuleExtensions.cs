using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using HomeMatch.Modules.Leasing.Infrastructure.Services;
using HomeMatch.Modules.Leasing.Presentation.Endpoints;
using HomeMatch.Shared.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMatch.Modules.Leasing;

public static class LeasingModuleExtensions
{
    public static IServiceCollection AddLeasingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<LeasingDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "leasing");
                });
            });
        }

        services.TryAddScoped<IRoomOwnershipService, DefaultRoomOwnershipService>();
        services.AddScoped<ILeasingService, LeasingService>();

        return services;
    }

    public static IEndpointRouteBuilder MapLeasingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/leasing").WithTags("Leasing");
        group.MapGet("/ping", () => Results.Ok(new { module = "Leasing", status = "Healthy", timestamp = DateTime.UtcNow }));

        endpoints.MapLeasingApiEndpoints();

        return endpoints;
    }
}
