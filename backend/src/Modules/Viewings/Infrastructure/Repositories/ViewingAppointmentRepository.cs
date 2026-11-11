using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Modules.Viewings.Domain.Entities;
using HomeMatch.Modules.Viewings.Domain.Enums;
using HomeMatch.Modules.Viewings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Viewings.Infrastructure.Repositories;

internal sealed class ViewingAppointmentRepository : IViewingAppointmentRepository
{
    private readonly ViewingsDbContext _dbContext;

    public ViewingAppointmentRepository(ViewingsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ViewingAppointment?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ViewingAppointments
            .Include(a => a.VirtualViewing)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> HasOverlappingAppointmentAsync(
        IReadOnlyCollection<long> listingIds,
        DateTimeOffset startTime,
        int durationMinutes,
        long? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
        {
            return false;
        }

        var endTime = startTime.AddMinutes(durationMinutes);
        var windowStart = startTime.AddMinutes(-180);

        var candidates = await _dbContext.ViewingAppointments
            .AsNoTracking()
            .Where(a => listingIds.Contains(a.ListingId)
                        && (a.Status == ViewingAppointmentStatus.Pending || a.Status == ViewingAppointmentStatus.Confirmed)
                        && (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value)
                        && a.AppointmentTime >= windowStart
                        && a.AppointmentTime < endTime)
            .Select(a => new { a.AppointmentTime, a.DurationMinutes })
            .ToListAsync(cancellationToken);

        return candidates.Any(a =>
            startTime < a.AppointmentTime.AddMinutes(a.DurationMinutes)
            && endTime > a.AppointmentTime);
    }

    public async Task<(IReadOnlyList<ViewingAppointment> Items, int TotalCount)> GetPagedByTenantAsync(
        long tenantId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ViewingAppointments
            .AsNoTracking()
            .Include(a => a.VirtualViewing)
            .Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(a => a.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.AppointmentTime)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<ViewingAppointment> Items, int TotalCount)> GetPagedByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
        {
            return ([], 0);
        }

        var query = _dbContext.ViewingAppointments
            .AsNoTracking()
            .Include(a => a.VirtualViewing)
            .Where(a => listingIds.Contains(a.ListingId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(a => a.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.AppointmentTime)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> CountByTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ViewingAppointments
            .AsNoTracking()
            .CountAsync(a => a.TenantId == tenantId, cancellationToken);
    }

    public async Task AddAsync(ViewingAppointment appointment, CancellationToken cancellationToken = default)
    {
        await _dbContext.ViewingAppointments.AddAsync(appointment, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
