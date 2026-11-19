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

    public async Task<long> GetTotalDepositAmountAsync(long landlordId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.LandlordId == landlordId && c.Status == ContractStatus.Active)
            .SumAsync(c => c.DepositAmount, cancellationToken);
    }

    public async Task<bool> ActivateContractOnPaymentAsync(
        long contractId,
        long paidAmount,
        long actorId,
        CancellationToken cancellationToken = default)
    {
        var contract = await _dbContext.Contracts
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
        {
            return false;
        }

        if (contract.Status == ContractStatus.Active)
        {
            return true;
        }

        if (contract.Status != ContractStatus.Signed)
        {
            return false;
        }

        var wasDepositPending = contract.DepositStatus == DepositStatus.Pending;
        var now = DateTimeOffset.UtcNow;
        var result = contract.ActivateOnPayment(paidAmount, now);
        if (result.IsFailure)
        {
            return false;
        }

        if (wasDepositPending && contract.DepositStatus == DepositStatus.Paid && contract.DepositAmount > 0)
        {
            var txResult = Domain.Entities.DepositTransaction.Create(
                contract.Id,
                DepositTransactionType.Received,
                contract.DepositAmount,
                "Thu tiền cọc qua thanh toán trực tuyến",
                actorId);

            if (txResult.IsSuccess)
            {
                _dbContext.DepositTransactions.Add(txResult.Value);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
