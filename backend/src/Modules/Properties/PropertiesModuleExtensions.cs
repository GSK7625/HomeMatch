using HomeMatch.Modules.Properties.Application.Features.Catalogs.GetAmenities;
using HomeMatch.Modules.Properties.Application.Features.Catalogs.GetProvinces;
using HomeMatch.Modules.Properties.Application.Features.Catalogs.GetWards;
using HomeMatch.Modules.Properties.Application.Features.Properties.CreateProperty;
using HomeMatch.Modules.Properties.Application.Features.Properties.GetMyProperties;
using HomeMatch.Modules.Properties.Application.Features.Properties.GetPropertyById;
using HomeMatch.Modules.Properties.Application.Features.Properties.UpdateProperty;
using HomeMatch.Modules.Properties.Application.Features.Rooms.CreateRoom;
using HomeMatch.Modules.Properties.Application.Features.Rooms.GetPropertyRooms;
using HomeMatch.Modules.Properties.Application.Features.Rooms.GetRoomById;
using HomeMatch.Modules.Properties.Application.Features.Rooms.SetRoomStatus;
using HomeMatch.Modules.Properties.Application.Features.Rooms.UpdateRoom;
using HomeMatch.Modules.Properties.Application.Features.Tariffs.GetPropertyTariffs;
using HomeMatch.Modules.Properties.Application.Features.Tariffs.SetPropertyTariffs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Infrastructure.Persistence;
using HomeMatch.Modules.Properties.Infrastructure.Geocoding;
using HomeMatch.Modules.Properties.Infrastructure.Persistence.Repositories;
using HomeMatch.Modules.Properties.Presentation.Endpoints;
using HomeMatch.Shared.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Properties;

public static class PropertiesModuleExtensions
{
    public static IServiceCollection AddPropertiesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GeocodingOptions>()
            .Bind(configuration.GetSection(GeocodingOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.Query)
                && options.BaseUrl.EndsWith('/'), "Geocoding:BaseUrl must be an HTTPS URL ending with '/'.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.UserAgent)
                && System.Net.Http.Headers.ProductInfoHeaderValue.TryParse(options.UserAgent, out _),
                "Geocoding:UserAgent must identify the application, for example HomeMatch/1.0.")
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 30,
                "Geocoding:TimeoutSeconds must be between 1 and 30.")
            .ValidateOnStart();
        services.AddHttpClient(NominatimAddressGeocoder.ClientName);
        services.AddSingleton<IAddressGeocoder, NominatimAddressGeocoder>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<PropertiesDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "properties"));
            }
        });

        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<ITariffRepository, TariffRepository>();
        services.AddScoped<GetAmenitiesHandler>();
        services.AddScoped<GetProvincesHandler>();
        services.AddScoped<GetWardsHandler>();
        services.AddScoped<CreatePropertyHandler>();
        services.AddScoped<GetMyPropertiesHandler>();
        services.AddScoped<GetPropertyByIdHandler>();
        services.AddScoped<UpdatePropertyHandler>();
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<GetPropertyRoomsHandler>();
        services.AddScoped<GetRoomByIdHandler>();
        services.AddScoped<SetRoomStatusHandler>();
        services.AddScoped<UpdateRoomHandler>();
        services.AddScoped<GetPropertyTariffsHandler>();
        services.AddScoped<SetPropertyTariffsHandler>();
        services.AddScoped<ICatalogReader, CatalogReader>();
        services.AddScoped<IPropertyOwnershipReader, PropertyOwnershipReader>();
        services.AddScoped<IRoomListingReader, RoomListingReader>();

        return services;
    }

    public static IEndpointRouteBuilder MapPropertiesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var propertiesGroup = endpoints.MapGroup("/api/v1/properties").WithTags("Properties");
        propertiesGroup.MapGet("/ping", () => Results.Ok(new { module = "Properties", status = "Healthy", timestamp = DateTime.UtcNow }));
        propertiesGroup.MapPropertyEndpoints();
        propertiesGroup.MapRoomEndpoints();
        propertiesGroup.MapTariffEndpoints();

        endpoints.MapCatalogEndpoints();

        return endpoints;
    }
}
