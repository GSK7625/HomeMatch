using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HomeMatch.Modules.Properties.Infrastructure.Persistence;
using HomeMatch.Modules.Properties.Application;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Properties;

public static class PropertiesModuleExtensions
{
    public static IServiceCollection AddPropertiesModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<PropertiesDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "properties")));

        // Services — đăng ký theo Interface (tuân thủ DIP)
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ITariffService, TariffService>();
        services.AddScoped<IPropertyOwnershipReader, PropertyOwnershipReader>();
        services.AddScoped<ICatalogReader, CatalogReader>();

        // Cross-module contract
        services.AddScoped<IRoomListingReader, RoomListingReader>();

        return services;
    }
}
