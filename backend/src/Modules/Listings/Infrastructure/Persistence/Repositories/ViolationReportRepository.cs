using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Modules.Listings.Domain.Entities;
using HomeMatch.Modules.Listings.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Repositories;

public sealed class ViolationReportRepository(ListingsDbContext db) : IViolationReportRepository
{
    public void Add(ViolationReport report) => db.ViolationReports.Add(report);

    public Task<ViolationReport?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.ViolationReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ViolationReport> Items, int TotalCount)> GetPagedAsync(
        string? status,
        string? targetType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.ViolationReports.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(r => r.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            var normalizedTarget = targetType.Trim().ToUpperInvariant();
            query = query.Where(r => r.TargetType == normalizedTarget);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.Status == "PENDING")
            .ThenByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AdminReportStatsSnapshot> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken)
    {
        var rows = await db.ViolationReports.AsNoTracking()
            .Select(r => new { r.Status, r.CreatedAt })
            .ToListAsync(cancellationToken);

        return new AdminReportStatsSnapshot(
            TotalReports: rows.Count,
            PendingReports: rows.Count(r => r.Status == "PENDING"),
            ResolvedReports: rows.Count(r => r.Status == "RESOLVED"),
            DismissedReports: rows.Count(r => r.Status == "DISMISSED"),
            NewReportsInPeriod: fromUtc.HasValue ? rows.Count(r => r.CreatedAt >= fromUtc.Value) : rows.Count);
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
