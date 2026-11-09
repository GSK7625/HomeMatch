using HomeMatch.Modules.Viewings.Application.Commands.BookViewingAppointment;
using HomeMatch.Modules.Viewings.Application.Commands.CancelViewingAppointment;
using HomeMatch.Modules.Viewings.Application.Commands.CompleteViewingAppointment;
using HomeMatch.Modules.Viewings.Application.Commands.ConfirmViewingAppointment;
using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointmentById;
using HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointments;
using HomeMatch.Modules.Viewings.Infrastructure.Persistence;
using HomeMatch.Modules.Viewings.Infrastructure.Repositories;
using HomeMatch.Modules.Viewings.Infrastructure.Services;
using HomeMatch.Modules.Viewings.Presentation.Endpoints;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Viewings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMatch.Modules.Viewings;

public static class ViewingsModuleExtensions
{
    public static IServiceCollection AddViewingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ViewingsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", ViewingsDbContext.Schema);
            });
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // Repositories & Cross-Module Contracts
        services.AddScoped<IViewingAppointmentRepository, ViewingAppointmentRepository>();
        services.AddScoped<IViewingStatsReader, ViewingStatsReader>();
        services.TryAddScoped<IListingContractReader, FallbackListingContractReader>();

        // Use Case Handlers
        services.AddScoped<BookViewingAppointmentHandler>();
        services.AddScoped<ConfirmViewingAppointmentHandler>();
        services.AddScoped<CancelViewingAppointmentHandler>();
        services.AddScoped<CompleteViewingAppointmentHandler>();
        services.AddScoped<GetViewingAppointmentsHandler>();
        services.AddScoped<GetViewingAppointmentByIdHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapViewingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/viewings").WithTags("Viewings");
        group.MapGet("/ping", () => Results.Ok(new { module = "Viewings", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapViewingAppointmentEndpoints();
        return endpoints;
    }
}
