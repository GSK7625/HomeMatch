using HomeMatch.Modules.Notifications.Application.Commands.MarkAllNotificationsRead;
using HomeMatch.Modules.Notifications.Application.Commands.MarkNotificationRead;
using HomeMatch.Modules.Notifications.Application.Interfaces;
using HomeMatch.Modules.Notifications.Application.Queries.GetNotifications;
using HomeMatch.Modules.Notifications.Infrastructure.Persistence;
using HomeMatch.Modules.Notifications.Infrastructure.Repositories;
using HomeMatch.Modules.Notifications.Infrastructure.Services;
using HomeMatch.Modules.Notifications.Presentation.Endpoints;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Notifications;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<NotificationsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", NotificationsDbContext.Schema);
            });
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();

        services.AddScoped<GetNotificationsHandler>();
        services.AddScoped<MarkNotificationReadHandler>();
        services.AddScoped<MarkAllNotificationsReadHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications").WithTags("Notifications");
        group.MapGet("/ping", () => Results.Ok(new { module = "Notifications", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapNotificationFeatureEndpoints();
        return endpoints;
    }
}
