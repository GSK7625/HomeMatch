using HomeMatch.Modules.Messaging.Application.Commands.MarkConversationRead;
using HomeMatch.Modules.Messaging.Application.Commands.SaveMessageDraft;
using HomeMatch.Modules.Messaging.Application.Commands.SendMessage;
using HomeMatch.Modules.Messaging.Application.Commands.StartOrGetConversation;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Application.Queries.GetConversationMessages;
using HomeMatch.Modules.Messaging.Application.Queries.GetConversations;
using HomeMatch.Modules.Messaging.Infrastructure.Persistence;
using HomeMatch.Modules.Messaging.Infrastructure.Repositories;
using HomeMatch.Modules.Messaging.Infrastructure.Services;
using HomeMatch.Modules.Messaging.Presentation.Endpoints;
using HomeMatch.Modules.Messaging.Presentation.Hubs;
using HomeMatch.Shared.Contracts.Messaging;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Messaging;

public static class MessagingModuleExtensions
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<MessagingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", MessagingDbContext.Schema);
            });
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // SignalR Realtime Engine (.NET 10 built-in)
        services.AddSignalR();

        // Repositories & Cross-Module Contracts
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessagingStatsReader, MessagingStatsReader>();
        services.AddScoped<IChatRealtimeNotifier, SignalRChatRealtimeNotifier>();
        services.AddScoped<INotificationRealtimeNotifier, SignalRNotificationRealtimeNotifier>();

        // Use Case Handlers
        services.AddScoped<StartOrGetConversationHandler>();
        services.AddScoped<SendMessageHandler>();
        services.AddScoped<MarkConversationReadHandler>();
        services.AddScoped<SaveMessageDraftHandler>();
        services.AddScoped<GetConversationsHandler>();
        services.AddScoped<GetConversationMessagesHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/messaging").WithTags("Messaging");
        group.MapGet("/ping", () => Results.Ok(new { module = "Messaging", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapMessagingFeatureEndpoints();
        group.MapHub<ChatHub>("/hub");
        endpoints.MapHub<ChatHub>("/hubs/chat");
        return endpoints;
    }
}
