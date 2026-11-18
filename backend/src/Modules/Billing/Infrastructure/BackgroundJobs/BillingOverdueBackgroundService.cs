using HomeMatch.Modules.Billing.Application.Commands.ProcessInvoiceOverdue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Billing.Infrastructure.BackgroundJobs;

public sealed class BillingOverdueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BillingOverdueBackgroundService> _logger;

    public BillingOverdueBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BillingOverdueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue("BillingBackgroundJobs:OverdueCheckIntervalMinutes", 30);
        if (intervalMinutes < 1)
        {
            intervalMinutes = 30;
        }

        var interval = TimeSpan.FromMinutes(intervalMinutes);
        _logger.LogInformation(
            "BillingOverdueBackgroundService đã khởi động. Chu kỳ quét: {Minutes} phút.",
            intervalMinutes);

        // Đợi 20 giây khi khởi động server để đảm bảo DB và migration đã hoàn tất
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProcessInvoiceOverdueHandler>();

                var result = await handler.HandleAsync(new ProcessInvoiceOverdueCommand(), stoppingToken);
                if (result.IsFailure)
                {
                    _logger.LogWarning("Xử lý quét quá hạn kết thúc với lỗi: {Error}", result.Error.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định trong BillingOverdueBackgroundService: {Message}", ex.Message);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        _logger.LogInformation("BillingOverdueBackgroundService đã dừng.");
    }
}
