using HomeMatch.Modules.Listings.Application.Features.ListingImages.AddListingImage;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.DeleteListingImage;
using HomeMatch.Modules.Listings.Application.Features.ListingImages.UpdateListingImage;
using HomeMatch.Modules.Listings.Application.Features.Listings.CreateListing;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetMyListingById;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetMyListings;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListingById;
using HomeMatch.Modules.Listings.Application.Features.Listings.GetPublicListings;
using HomeMatch.Modules.Listings.Application.Features.Listings.SearchListings;
using HomeMatch.Modules.Listings.Application.Features.Listings.HideListing;
using HomeMatch.Modules.Listings.Application.Features.Listings.SubmitListing;
using HomeMatch.Modules.Listings.Application.Features.Moderation.ApproveListing;
using HomeMatch.Modules.Listings.Application.Features.Moderation.RejectListing;
using HomeMatch.Modules.Listings.Application.Features.Moderation.GetPendingListings;
using HomeMatch.Modules.Listings.Application.Features.Moderation.GetReviewListing;
using HomeMatch.Modules.Listings.Application.Features.Listings.UpdateListing;
using HomeMatch.Modules.Listings.Application.Features.SavedListings.GetSavedListings;
using HomeMatch.Modules.Listings.Application.Features.SavedListings.ToggleSaveListing;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Modules.Listings.Infrastructure.Persistence.Repositories;
using HomeMatch.Modules.Listings.Infrastructure.Services;
using HomeMatch.Modules.Listings.Infrastructure.BackgroundServices;
using HomeMatch.Modules.Listings.Infrastructure.Services;
using HomeMatch.Modules.Listings.Presentation.Endpoints;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Listings;

public static class ListingsModuleExtensions
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ListingsDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseNetTopologySuite();
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "listings");
                });
            }
        });

        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddHostedService<ListingExpirationService>();
        services.AddScoped<IListingImageRepository, ListingImageRepository>();
        services.AddScoped<ISavedListingRepository, SavedListingRepository>();

        // Cross-module writers & readers
        services.AddScoped<IListingVisibilityWriter, ListingVisibilityWriter>();
        services.AddScoped<IListingContractReader, ListingContractReader>();
        services.AddScoped<ITenantListingActivityReader, TenantListingActivityReader>();

        // Listings Handlers
        services.AddScoped<CreateListingHandler>();
        services.AddScoped<UpdateListingHandler>();
        services.AddScoped<SubmitListingHandler>();
        services.AddScoped<ApproveListingHandler>();
        services.AddScoped<RejectListingHandler>();
        services.AddScoped<GetPendingListingsHandler>();
        services.AddScoped<GetReviewListingHandler>();
        services.AddScoped<HideListingHandler>();
        services.AddScoped<GetPublicListingsHandler>();
        services.AddScoped<SearchListingsHandler>();
        services.AddScoped<GetPublicListingByIdHandler>();
        services.AddScoped<GetMyListingsHandler>();
        services.AddScoped<GetMyListingByIdHandler>();

        // Listing Images Handlers
        services.AddScoped<AddListingImageHandler>();
        services.AddScoped<UpdateListingImageHandler>();
        services.AddScoped<DeleteListingImageHandler>();

        // Saved Listings Handlers
        services.AddScoped<ToggleSaveListingHandler>();
        services.AddScoped<GetSavedListingsHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapListingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/listings").WithTags("Listings")
            .AddEndpointFilter<ListingRequestFilter>()
            .DisableValidation();
        group.MapGet("/ping", () => Results.Ok(new { module = "Listings", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapListingEndpoints();
        group.MapListingImageEndpoints();
        endpoints.MapAdminListingEndpoints();

        return endpoints;
    }
}
