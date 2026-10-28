using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Modules.Listings.Application;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings;

public static class ListingsModuleExtensions
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ListingsDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "listings");
            }));
        services.AddScoped<IListingWriteService, ListingWriteService>();
        services.AddScoped<IListingReadService, ListingReadService>();
        services.AddScoped<IListingImageService, ListingImageService>();
        services.AddScoped<IListingVisibilityWriter, ListingVisibilityWriter>();
        return services;
    }

}
