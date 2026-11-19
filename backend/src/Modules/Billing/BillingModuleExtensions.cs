using HomeMatch.Modules.Billing.Application.Commands.CancelInvoice;
using HomeMatch.Modules.Billing.Application.Commands.CreateInvoice;
using HomeMatch.Modules.Billing.Application.Commands.CreatePayment;
using HomeMatch.Modules.Billing.Application.Commands.PayContractDeposit;
using HomeMatch.Modules.Billing.Application.Commands.ProcessInvoiceOverdue;
using HomeMatch.Modules.Billing.Application.Commands.ProcessPaymentCallback;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Queries.GetDebtInvoices;
using HomeMatch.Modules.Billing.Application.Queries.GetDebtSummary;
using HomeMatch.Modules.Billing.Application.Queries.GetFinancialSummary;
using HomeMatch.Modules.Billing.Application.Queries.GetInvoiceById;
using HomeMatch.Modules.Billing.Application.Queries.GetInvoices;
using HomeMatch.Modules.Billing.Application.Queries.GetMonthlyCashflow;
using HomeMatch.Modules.Billing.Application.Queries.GetPaymentById;
using HomeMatch.Modules.Billing.Application.Queries.GetPropertyFinancialReport;
using HomeMatch.Modules.Billing.Infrastructure.BackgroundJobs;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using HomeMatch.Modules.Billing.Infrastructure.Repositories;
using HomeMatch.Modules.Billing.Infrastructure.Services;
using HomeMatch.Modules.Billing.Presentation.Endpoints;
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

        // VNPay Options & Gateway
        services.Configure<VNPayOptions>(configuration.GetSection(VNPayOptions.SectionName));
        services.AddScoped<IVNPayGateway, VNPayGateway>();

        // Interfaces & Repositories
        services.AddScoped<IBillingDbContext>(sp => sp.GetRequiredService<BillingDbContext>());
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ILandlordDashboardService, LandlordDashboardService>();

        // Application Handlers - Invoices
        services.AddScoped<CreateInvoiceHandler>();
        services.AddScoped<CancelInvoiceHandler>();
        services.AddScoped<GetInvoiceByIdHandler>();
        services.AddScoped<GetInvoicesHandler>();

        // Application Handlers - Payments
        services.AddScoped<CreatePaymentHandler>();
        services.AddScoped<PayContractDepositHandler>();
        services.AddScoped<ProcessPaymentCallbackHandler>();
        services.AddScoped<GetPaymentByIdHandler>();

        // Application Handlers - Debts & Background Jobs
        services.AddScoped<ProcessInvoiceOverdueHandler>();
        services.AddScoped<GetDebtSummaryHandler>();
        services.AddScoped<GetDebtInvoicesHandler>();

        // Application Handlers - Reports
        services.AddScoped<GetFinancialSummaryHandler>();
        services.AddScoped<GetPropertyFinancialReportHandler>();
        services.AddScoped<GetMonthlyCashflowHandler>();

        // Background Services
        services.AddHostedService<BillingOverdueBackgroundService>();

        return services;
    }

    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/billing").WithTags("Billing");

        group.MapBillingEndpointsInternal();
        group.MapInvoiceEndpoints();
        group.MapPaymentEndpoints();
        group.MapDebtEndpoints();
        group.MapBillingReportEndpoints();

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
