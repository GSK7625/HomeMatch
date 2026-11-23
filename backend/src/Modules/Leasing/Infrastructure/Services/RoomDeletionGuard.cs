using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Leasing;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Leasing.Infrastructure.Services;

public sealed class RoomDeletionGuard(LeasingDbContext db) : IRoomDeletionGuard
{
    public Task<bool> HasReservingContractsAsync(long roomId, CancellationToken cancellationToken) =>
        db.Contracts.AsNoTracking().AnyAsync(contract => contract.RoomId == roomId
            && (contract.Status == ContractStatus.PendingSignature || contract.Status == ContractStatus.Signed
                || contract.Status == ContractStatus.Active || contract.Status == ContractStatus.ExpiredPendingHandover
                || contract.Status == ContractStatus.HandoverOverdue
                || (contract.Status == ContractStatus.Terminated && contract.HandoverConfirmedAt == null)), cancellationToken);
}
