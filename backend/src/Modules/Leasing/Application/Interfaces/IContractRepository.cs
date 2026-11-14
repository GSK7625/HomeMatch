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

    /// <summary>
    /// Lấy danh sách hợp đồng PENDING_SIGNATURE đã quá hạn ký điện tử
    /// </summary>
    Task<IReadOnlyList<Contract>> GetPendingSignatureExpiredContractsAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách hợp đồng SIGNED đã đến hoặc qua ngày bắt đầu thuê (StartDate) cần kích hoạt ACTIVE
    /// </summary>
    Task<IReadOnlyList<Contract>> GetSignedContractsReadyForActivationAsync(
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách hợp đồng ACTIVE đã qua ngày kết thúc thuê (EndDate < today) cần chuyển sang EXPIRED_PENDING_HANDOVER
    /// </summary>
    Task<IReadOnlyList<Contract>> GetActiveContractsPastEndDateAsync(
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách hợp đồng EXPIRED_PENDING_HANDOVER đã quá hạn bàn giao phòng cần chuyển sang HANDOVER_OVERDUE
    /// </summary>
    Task<IReadOnlyList<Contract>> GetExpiredContractsPastHandoverDueAsync(
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách hợp đồng ACTIVE sắp hết hạn thuê trong khoảng thời gian chỉ định của Chủ nhà (dành cho API truy vấn)
    /// </summary>
    Task<IReadOnlyList<Contract>> GetExpiringContractsForLandlordAsync(
        long landlordId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách hợp đồng ACTIVE sắp hết hạn thuê cần gửi thông báo nhắc nhở (chưa gửi hôm nay)
    /// </summary>
    Task<IReadOnlyList<Contract>> GetContractsNeedingExpirationReminderAsync(
        DateOnly asOfDate,
        int daysWindow,
        CancellationToken cancellationToken = default);

    Task AddAsync(Contract contract, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
