using HomeMatch.Modules.Notifications.Infrastructure.Email;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Notifications;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Transactional mail crosses modules only through the Shared contract.
        services.AddSingleton<IValidateOptions<GmailSmtpOptions>, GmailSmtpOptionsValidator>();
        services.AddOptions<GmailSmtpOptions>().Bind(configuration.GetSection("Email:Smtp"))
            .Configure(options => options.Enabled = configuration.GetValue<bool>("Email:Enabled")).ValidateOnStart();
        services.AddSingleton<ISmtpSubmissionTransport, MailKitSmtpTransport>();
        services.AddScoped<ITransactionalEmailSender, GmailSmtpEmailSender>();
        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications").WithTags("Notifications");
        group.MapGet("/ping", () => Results.Ok(new { module = "Notifications", status = "Healthy", timestamp = DateTime.UtcNow }));
        return endpoints;
    }
}
