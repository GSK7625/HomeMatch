using HomeMatch.Modules.Billing.Application.Commands.CancelInvoice;
using HomeMatch.Modules.Billing.Application.Commands.CreateInvoice;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Queries.GetInvoiceById;
using HomeMatch.Modules.Billing.Application.Queries.GetInvoices;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using HomeMatch.Modules.Billing.Infrastructure.Repositories;
using HomeMatch.Modules.Billing.Presentation.Endpoints;
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

        // Interfaces & Repositories
        services.AddScoped<IBillingDbContext>(sp => sp.GetRequiredService<BillingDbContext>());
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Application Handlers
        services.AddScoped<CreateInvoiceHandler>();
        services.AddScoped<CancelInvoiceHandler>();
        services.AddScoped<GetInvoiceByIdHandler>();
        services.AddScoped<GetInvoicesHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/billing").WithTags("Billing");
        group.MapBillingEndpointsInternal();
        group.MapInvoiceEndpoints();
        return endpoints;
    }
}
