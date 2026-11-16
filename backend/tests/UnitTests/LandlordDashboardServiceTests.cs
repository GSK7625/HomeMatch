using System.Net.Sockets;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using HomeMatch.Modules.Billing.Infrastructure.Persistence.Seed;
using HomeMatch.Modules.Billing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.UnitTests;

public class LandlordDashboardServiceTests
{
    private static bool IsPostgreSqlAvailable()
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync("127.0.0.1", 5432);
            if (!connectTask.Wait(TimeSpan.FromMilliseconds(500)))
            {
                return false;
            }
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<BillingDbContext> GetSeededDbContextAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<BillingDbContext>(options =>
        {
            options.UseNpgsql("Host=localhost;Port=5432;Database=homematch_db;Username=homematch_user;Password=homematch_password;");
        });

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<BillingDbContext>();
        await db.Database.EnsureCreatedAsync();
        await BillingDataSeeder.SeedAsync(provider);
        return db;
    }

    [Fact]
    public async Task GetDashboardAsync_WithAllBuildings_ShouldCalculateAccurateMetrics()
    {
        if (!IsPostgreSqlAvailable()) return;

        // Arrange
        var db = await GetSeededDbContextAsync();
        var service = new LandlordDashboardService(db);

        // Act
        var result = await service.GetDashboardAsync(buildingId: "all", period: "11/2026");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12, result.Overview.TotalRooms);
        Assert.Equal(4, result.Overview.VacantRooms);
        Assert.Equal(8, result.Overview.RentedRooms);
        Assert.True(result.Overview.OccupancyRate > 60);

        Assert.True(result.Financials.TotalReceivables > 0);
        Assert.True(result.Financials.TotalCollected > 0);
        Assert.Equal(
            result.Financials.TotalReceivables - result.Financials.TotalCollected,
            result.Financials.RemainingDebt);

        Assert.True(result.Financials.CollectionRate > 0 && result.Financials.CollectionRate <= 100);
        Assert.NotEmpty(result.Deposits.Items);
        Assert.True(result.Deposits.TotalActiveDeposits > 0);
        Assert.NotEmpty(result.ViewingsToday);
    }

    [Fact]
    public async Task GetDashboardAsync_WithSpecificBuilding_ShouldFilterCorrectly()
    {
        if (!IsPostgreSqlAvailable()) return;

        // Arrange
        var db = await GetSeededDbContextAsync();
        var service = new LandlordDashboardService(db);

        // Act
        var result = await service.GetDashboardAsync(buildingId: "bld-001", period: "11/2026");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Overview.TotalRooms);
        Assert.Equal(2, result.Overview.VacantRooms);
        Assert.Equal(3, result.Overview.RentedRooms);

        Assert.All(result.DebtAging.Items, item =>
        {
            Assert.Contains(item.RoomName, new[] { "P.101 - Studio Ban công", "P.102 - 1N1K Thoáng", "P.201 - Duplex Gác lửng" });
        });
    }

    [Fact]
    public async Task GetDashboardAsync_DebtAgingBuckets_ShouldSumCorrectly()
    {
        if (!IsPostgreSqlAvailable()) return;

        // Arrange
        var db = await GetSeededDbContextAsync();
        var service = new LandlordDashboardService(db);

        // Act
        var result = await service.GetDashboardAsync(buildingId: "all", period: "11/2026");

        // Assert
        var debt = result.DebtAging;
        Assert.Equal(debt.TotalDebt, debt.CurrentAmount + debt.OverdueDebt);
        Assert.Equal(debt.OverdueDebt, debt.Overdue1To30Days + debt.Overdue31To60Days + debt.OverdueOver60Days);
    }
}
