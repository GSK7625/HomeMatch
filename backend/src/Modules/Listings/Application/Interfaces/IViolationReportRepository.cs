using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public interface IViolationReportRepository
{
    void Add(ViolationReport report);
    Task<ViolationReport?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<ViolationReport> Items, int TotalCount)> GetPagedAsync(
        string? status,
        string? targetType,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<AdminReportStatsSnapshot> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken);
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record AdminReportStatsSnapshot(
    int TotalReports,
    int PendingReports,
    int ResolvedReports,
    int DismissedReports,
    int NewReportsInPeriod);
