using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Entities;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Leasing.Infrastructure.Repositories;

internal sealed class DepositTransactionRepository : IDepositTransactionRepository
{
    private readonly LeasingDbContext _dbContext;

    public DepositTransactionRepository(LeasingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DepositTransaction>> GetByContractIdAsync(
        long contractId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DepositTransactions
            .AsNoTracking()
            .Where(dt => dt.ContractId == contractId)
            .OrderBy(dt => dt.CreatedAt)
            .ThenBy(dt => dt.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DepositTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.DepositTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
