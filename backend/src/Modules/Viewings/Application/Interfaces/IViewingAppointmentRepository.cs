using HomeMatch.Modules.Viewings.Domain.Entities;

namespace HomeMatch.Modules.Viewings.Application.Interfaces;

public interface IViewingAppointmentRepository
{
    Task<ViewingAppointment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingAppointmentAsync(
        IReadOnlyCollection<long> listingIds,
        DateTimeOffset startTime,
        int durationMinutes,
        long? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ViewingAppointment> Items, int TotalCount)> GetPagedByTenantAsync(
        long tenantId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ViewingAppointment> Items, int TotalCount)> GetPagedByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    Task AddAsync(ViewingAppointment appointment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
