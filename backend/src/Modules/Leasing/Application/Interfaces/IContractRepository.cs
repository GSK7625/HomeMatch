using HomeMatch.Modules.Leasing.Domain.Entities;

namespace HomeMatch.Modules.Leasing.Application.Interfaces;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Contract?> GetByCodeAsync(string contractCode, CancellationToken cancellationToken = default);

    Task<Contract?> GetDetailsByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải hợp đồng kèm thông tin chi tiết và danh sách giao dịch cọc để thực hiện cập nhật.
    /// </summary>
    Task<Contract?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingOccupancyAsync(
        long roomId,
        DateOnly startDate,
        DateOnly endDate,
        long? excludeContractId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Contract> Items, int TotalCount)> GetPagedByLandlordAsync(
        long landlordId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Contract> Items, int TotalCount)> GetPagedByTenantAsync(
        long tenantId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Contract>> GetActiveExpiringContractsAsync(
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(Contract contract, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
