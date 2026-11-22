using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Admin;

namespace HomeMatch.Modules.Listings.Application.Features.AdminDashboard;

public sealed record GetAdminDashboardQuery(string? Period = "all");

public sealed record AdminDashboardResult(
    string Period,
    DateTimeOffset? FromUtc,
    AdminIdentityStats Users,
    AdminPropertyStats Properties,
    AdminListingStatsSnapshot Listings,
    AdminReportStatsSnapshot Reports);

public sealed class GetAdminDashboardHandler(
    IAdminIdentityStatsReader identityStats,
    IAdminPropertyStatsReader propertyStats,
    IListingRepository listings,
    IViolationReportRepository reports)
{
    public async Task<Result<AdminDashboardResult>> HandleAsync(
        GetAdminDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalized = (query.Period ?? "all").Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? fromUtc = normalized switch
        {
            "today" => new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero),
            "week" => now.AddDays(-7),
            "month" => now.AddDays(-30),
            "year" => now.AddDays(-365),
            _ => null
        };

        var periodKey = fromUtc.HasValue ? normalized : "all";

        // Thực thi tuần tự bất đồng bộ theo quy tắc backend-rules.md
        var userStats = await identityStats.GetStatsAsync(fromUtc, cancellationToken);
        var propStats = await propertyStats.GetStatsAsync(fromUtc, cancellationToken);
        var listingStats = await listings.GetAdminStatsAsync(fromUtc, cancellationToken);
        var reportStats = await reports.GetStatsAsync(fromUtc, cancellationToken);

        return Result.Success(new AdminDashboardResult(
            Period: periodKey,
            FromUtc: fromUtc,
            Users: userStats,
            Properties: propStats,
            Listings: listingStats,
            Reports: reportStats));
    }
}
