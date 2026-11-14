using HomeMatch.Modules.Leasing.Application.Commands.ProcessContractLifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Infrastructure.BackgroundJobs;

public sealed class ContractLifecycleBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContractLifecycleBackgroundService> _logger;

    public ContractLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ContractLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue("LeasingBackgroundJobs:LifecycleIntervalMinutes", 15);
        if (intervalMinutes < 1)
        {
            intervalMinutes = 15;
        }

        var interval = TimeSpan.FromMinutes(intervalMinutes);
        _logger.LogInformation(
            "ContractLifecycleBackgroundService đã khởi động. Chu kỳ quét: {Minutes} phút.",
            intervalMinutes);

        // Đợi 15 giây khi khởi động server
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
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
                var handler = scope.ServiceProvider.GetRequiredService<ProcessContractLifecycleHandler>();

                var result = await handler.HandleAsync(new ProcessContractLifecycleCommand(), stoppingToken);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Xử lý vòng đời hợp đồng kết thúc với lỗi: {Error}",
                        result.Error.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định trong ContractLifecycleBackgroundService: {Message}", ex.Message);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        _logger.LogInformation("ContractLifecycleBackgroundService đã dừng.");
    }
}
