using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using HomeMatch.Modules.Billing.Infrastructure.Services;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Billing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Billing;

public static class BillingModuleExtensions
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<BillingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", BillingDbContext.Schema);
            });
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<ILandlordDashboardService, LandlordDashboardService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/billing").WithTags("Billing");

        group.MapGet("/ping", () => Results.Ok(new
        {
            module = "Billing",
            status = "Healthy",
            timestamp = DateTime.UtcNow
        }));

        group.MapGet("/landlord-dashboard", async (
            string? buildingId,
            string? period,
            ILandlordDashboardService dashboardService,
            CancellationToken cancellationToken) =>
        {
            var data = await dashboardService.GetDashboardAsync(buildingId, period, cancellationToken);
            return Results.Ok(ApiResponse<LandlordDashboardDto>.Success(data));
        })
        .WithName("GetLandlordDashboard")
        .WithSummary("Lấy dữ liệu tổng quan Dashboard và Báo cáo tài chính cho Chủ nhà")
        .Produces<ApiResponse<LandlordDashboardDto>>(StatusCodes.Status200OK);

        // Alias route: /api/v1/landlord/dashboard for direct semantic access
        endpoints.MapGet("/api/v1/landlord/dashboard", async (
            string? buildingId,
            string? period,
            ILandlordDashboardService dashboardService,
            CancellationToken cancellationToken) =>
        {
            var data = await dashboardService.GetDashboardAsync(buildingId, period, cancellationToken);
            return Results.Ok(ApiResponse<LandlordDashboardDto>.Success(data));
        })
        .WithTags("Landlord")
        .WithName("GetLandlordDashboardDirect")
        .WithSummary("Lấy dữ liệu tổng quan Dashboard Chủ nhà")
        .Produces<ApiResponse<LandlordDashboardDto>>(StatusCodes.Status200OK);

        return endpoints;
    }
}
