using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Leasing;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Leasing.Infrastructure.Services;

internal sealed class LeasingContractReader : ILeasingContractReader
{
    private readonly LeasingDbContext _dbContext;

    public LeasingContractReader(LeasingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LeasingContractInfo?> GetContractByIdAsync(long contractId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.Id == contractId)
            .Select(c => new LeasingContractInfo(
                c.Id,
                c.ContractCode,
                c.RoomId,
                c.TenantId,
                c.LandlordId,
                c.StartDate,
                c.EndDate,
                c.MonthlyRent,
                c.DepositAmount,
                c.BillingDueDay,
                c.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeasingContractInfo>> GetActiveContractsByLandlordIdAsync(long landlordId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.LandlordId == landlordId && c.Status == ContractStatus.Active)
            .Select(c => new LeasingContractInfo(
                c.Id,
                c.ContractCode,
                c.RoomId,
                c.TenantId,
                c.LandlordId,
                c.StartDate,
                c.EndDate,
                c.MonthlyRent,
                c.DepositAmount,
                c.BillingDueDay,
                c.Status))
            .ToListAsync(cancellationToken);
    }
}
