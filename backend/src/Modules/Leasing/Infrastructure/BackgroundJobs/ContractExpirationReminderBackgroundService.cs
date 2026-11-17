using HomeMatch.Modules.Leasing.Application.Commands.SendContractExpirationReminders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Infrastructure.BackgroundJobs;

public sealed class ContractExpirationReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContractExpirationReminderBackgroundService> _logger;

    public ContractExpirationReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ContractExpirationReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _configuration.GetValue("LeasingBackgroundJobs:ReminderIntervalHours", 6);
        if (intervalHours < 1)
        {
            intervalHours = 6;
        }

        var thresholdDays = _configuration.GetValue("LeasingBackgroundJobs:ReminderThresholdDays", 30);
        var interval = TimeSpan.FromHours(intervalHours);

        _logger.LogInformation(
            "ContractExpirationReminderBackgroundService đã khởi động. Chu kỳ quét: {Hours} giờ, Ngưỡng nhắc nhở: {Days} ngày.",
            intervalHours, thresholdDays);

        // Đợi 30 giây khi khởi động server
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
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
                var handler = scope.ServiceProvider.GetRequiredService<SendContractExpirationRemindersHandler>();

                var result = await handler.HandleAsync(
                    new SendContractExpirationRemindersCommand(DaysThreshold: thresholdDays),
                    stoppingToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Gửi nhắc nhở hết hạn hợp đồng kết thúc với lỗi: {Error}",
                        result.Error.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định trong ContractExpirationReminderBackgroundService: {Message}", ex.Message);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        _logger.LogInformation("ContractExpirationReminderBackgroundService đã dừng.");
    }
}
