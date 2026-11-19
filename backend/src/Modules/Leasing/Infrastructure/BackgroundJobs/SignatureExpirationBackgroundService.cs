using HomeMatch.Modules.Leasing.Application.Commands.ExpirePendingSignatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Infrastructure.BackgroundJobs;

public sealed class SignatureExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SignatureExpirationBackgroundService> _logger;

    public SignatureExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SignatureExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue("LeasingBackgroundJobs:SignatureExpirationIntervalMinutes", 5);
        if (intervalMinutes < 1)
        {
            intervalMinutes = 5;
        }

        var interval = TimeSpan.FromMinutes(intervalMinutes);
        _logger.LogInformation(
            "SignatureExpirationBackgroundService đã khởi động. Chu kỳ quét: {Minutes} phút.",
            intervalMinutes);

        // Đợi 10 giây khi khởi động server trước khi chạy lần đầu để DB hoàn tất migration
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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
                var handler = scope.ServiceProvider.GetRequiredService<ExpirePendingSignaturesHandler>();

                var result = await handler.HandleAsync(new ExpirePendingSignaturesCommand(), stoppingToken);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Quét chữ ký hết hạn kết thúc với lỗi: {Error}",
                        result.Error.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định trong SignatureExpirationBackgroundService: {Message}", ex.Message);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        _logger.LogInformation("SignatureExpirationBackgroundService đã dừng.");
    }
}
