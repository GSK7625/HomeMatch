using HomeMatch.Modules.Leasing.Application.Commands.ActivateContract;
using HomeMatch.Modules.Leasing.Application.Commands.CancelContract;
using HomeMatch.Modules.Leasing.Application.Commands.ConfirmHandover;
using HomeMatch.Modules.Leasing.Application.Commands.CreateContract;
using HomeMatch.Modules.Leasing.Application.Commands.ExpirePendingSignatures;
using HomeMatch.Modules.Leasing.Application.Commands.ProcessContractLifecycle;
using HomeMatch.Modules.Leasing.Application.Commands.RecordDepositTransaction;
using HomeMatch.Modules.Leasing.Application.Commands.RejectContract;
using HomeMatch.Modules.Leasing.Application.Commands.RequestSignatureOtp;
using HomeMatch.Modules.Leasing.Application.Commands.SendContractExpirationReminders;
using HomeMatch.Modules.Leasing.Application.Commands.SendContractForSignature;
using HomeMatch.Modules.Leasing.Application.Commands.SignContract;
using HomeMatch.Modules.Leasing.Application.Commands.TerminateContract;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractById;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractDetails;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractDocumentUrl;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractsByLandlord;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractsByTenant;
using HomeMatch.Modules.Leasing.Application.Queries.GetDepositBalance;
using HomeMatch.Modules.Leasing.Application.Queries.GetDepositTransactions;
using HomeMatch.Modules.Leasing.Application.Queries.GetExpiringContracts;
using HomeMatch.Modules.Leasing.Application.Queries.GetHandoverDetails;
using HomeMatch.Modules.Leasing.Infrastructure.BackgroundJobs;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using HomeMatch.Modules.Leasing.Infrastructure.Repositories;
using HomeMatch.Modules.Leasing.Infrastructure.Services;
using HomeMatch.Modules.Leasing.Presentation.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Modules.Leasing;

public static class LeasingModuleExtensions
{
    public static IServiceCollection AddLeasingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<LeasingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", LeasingDbContext.Schema);
            });
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // Repositories & Services
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IDepositTransactionRepository, DepositTransactionRepository>();
        services.AddScoped<ILeasingEmailService, LeasingEmailService>();

        // Storage & Cloudflare R2
        services.Configure<R2Settings>(configuration.GetSection(R2Settings.SectionName));
        services.AddSingleton<IContractDocumentStorage, R2DocumentStorageService>();

        // Use Case Handlers (Commands)
        services.AddScoped<CreateContractHandler>();
        services.AddScoped<SendContractForSignatureHandler>();
        services.AddScoped<RequestSignatureOtpHandler>();
        services.AddScoped<SignContractHandler>();
        services.AddScoped<RejectContractHandler>();
        services.AddScoped<CancelContractHandler>();
        services.AddScoped<ActivateContractHandler>();
        services.AddScoped<ConfirmHandoverHandler>();
        services.AddScoped<TerminateContractHandler>();
        services.AddScoped<RecordDepositTransactionHandler>();
        services.AddScoped<ExpirePendingSignaturesHandler>();
        services.AddScoped<ProcessContractLifecycleHandler>();
        services.AddScoped<SendContractExpirationRemindersHandler>();

        // Use Case Handlers (Queries)
        services.AddScoped<GetContractByIdHandler>();
        services.AddScoped<GetContractDetailsHandler>();
        services.AddScoped<GetContractDocumentHandler>();
        services.AddScoped<GetContractsByLandlordHandler>();
        services.AddScoped<GetContractsByTenantHandler>();
        services.AddScoped<GetDepositTransactionsHandler>();
        services.AddScoped<GetDepositBalanceHandler>();
        services.AddScoped<GetHandoverDetailsHandler>();
        services.AddScoped<GetExpiringContractsHandler>();

        // Background Jobs (Phase 7 - Automated Contract Lifecycle & Reminders)
        services.AddHostedService<SignatureExpirationBackgroundService>();
        services.AddHostedService<ContractLifecycleBackgroundService>();
        services.AddHostedService<ContractExpirationReminderBackgroundService>();

        return services;
    }

    public static IEndpointRouteBuilder MapLeasingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/leasing").WithTags("Leasing");
        group.MapGet("/ping", () => Results.Ok(new { module = "Leasing", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapContractEndpoints();
        return endpoints;
    }
}
