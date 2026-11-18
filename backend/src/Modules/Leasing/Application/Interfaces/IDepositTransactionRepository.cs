using HomeMatch.Modules.Leasing.Domain.Entities;

namespace HomeMatch.Modules.Leasing.Application.Interfaces;

public interface IDepositTransactionRepository
{
    Task<IReadOnlyList<DepositTransaction>> GetByContractIdAsync(long contractId, CancellationToken cancellationToken = default);

    Task AddAsync(DepositTransaction transaction, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
