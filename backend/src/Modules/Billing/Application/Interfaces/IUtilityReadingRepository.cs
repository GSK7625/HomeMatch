using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Domain.Entities;

namespace HomeMatch.Modules.Billing.Application.Interfaces;

public interface IUtilityReadingRepository
{
    Task<UtilityReading?> GetByRoomAndPeriodAsync(long roomId, int billingMonth, int billingYear, CancellationToken cancellationToken = default);
    Task<UtilityReading?> GetByContractAndPeriodAsync(long contractId, int billingMonth, int billingYear, CancellationToken cancellationToken = default);
    Task<UtilityReading?> GetLatestBeforePeriodAsync(long roomId, int billingMonth, int billingYear, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UtilityReading>> GetListAsync(UtilityReadingFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(UtilityReading reading, CancellationToken cancellationToken = default);
    Task UpdateAsync(UtilityReading reading, CancellationToken cancellationToken = default);
}
