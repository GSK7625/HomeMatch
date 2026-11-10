using HomeMatch.Modules.Listings.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Listings.Infrastructure.BackgroundServices;

public sealed class ListingExpirationService(IServiceScopeFactory scopes, ILogger<ListingExpirationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var listings = scope.ServiceProvider.GetRequiredService<IListingRepository>();
                await listings.ExpirePublishedAsync(DateTimeOffset.UtcNow, null, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Không cập nhật được tin hết hạn. Sẽ thử lại ở lượt tiếp theo.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
