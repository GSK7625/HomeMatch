using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Entities;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Leasing.Infrastructure.Repositories;

internal sealed class ContractRepository : IContractRepository
{
    private readonly LeasingDbContext _dbContext;

    public ContractRepository(LeasingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Contract?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Contract?> GetByCodeAsync(string contractCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .FirstOrDefaultAsync(c => c.ContractCode == contractCode, cancellationToken);
    }

    public async Task<Contract?> GetDetailsByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .Include(c => c.DepositTransactions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Contract?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .Include(c => c.DepositTransactions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> HasOverlappingOccupancyAsync(
        long roomId,
        DateOnly startDate,
        DateOnly endDate,
        long? excludeContractId = null,
        CancellationToken cancellationToken = default)
    {
        var reservingStatuses = new[]
        {
            ContractStatus.PendingSignature,
            ContractStatus.Signed,
            ContractStatus.Active,
            ContractStatus.ExpiredPendingHandover,
            ContractStatus.HandoverOverdue
        };

        return await _dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.RoomId == roomId
                && reservingStatuses.Contains(c.Status)
                && (!excludeContractId.HasValue || c.Id != excludeContractId.Value))
            .AnyAsync(c =>
                startDate <= (c.ActualOccupancyEndDate ?? c.EndDate)
                && endDate >= c.StartDate,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<Contract> Items, int TotalCount)> GetPagedByLandlordAsync(
        long landlordId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.LandlordId == landlordId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(c => c.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Contract> Items, int TotalCount)> GetPagedByTenantAsync(
        long tenantId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(c => c.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Contract>> GetActiveExpiringContractsAsync(
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .Where(c => c.Status == ContractStatus.Active && c.EndDate <= asOfDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contract>> GetPendingSignatureExpiredContractsAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .Where(c => c.Status == ContractStatus.PendingSignature
                     && c.SignatureExpiresAt != null
                     && c.SignatureExpiresAt <= asOf)
            .OrderBy(c => c.SignatureExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contract>> GetSignedContractsReadyForActivationAsync(
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .Where(c => c.Status == ContractStatus.Signed
                     && c.StartDate <= asOfDate)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contract>> GetActiveContractsPastEndDateAsync(
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .Where(c => c.Status == ContractStatus.Active
                     && c.EndDate < today)
            .OrderBy(c => c.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contract>> GetExpiredContractsPastHandoverDueAsync(
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var todayBeginning = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return await _dbContext.Contracts
            .Where(c => c.Status == ContractStatus.ExpiredPendingHandover
                     && c.HandoverDueAt != null
                     && c.HandoverDueAt < todayBeginning)
            .OrderBy(c => c.HandoverDueAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contract>> GetExpiringContractsForLandlordAsync(
        long landlordId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contracts
            .AsNoTracking()
            .Where(c => c.LandlordId == landlordId
                     && c.Status == ContractStatus.Active
                     && c.EndDate >= fromDate
                     && c.EndDate <= toDate)
            .OrderBy(c => c.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contract>> GetContractsNeedingExpirationReminderAsync(
        DateOnly asOfDate,
        int daysWindow,
        CancellationToken cancellationToken = default)
    {
        var maxDate = asOfDate.AddDays(daysWindow);
        var asOfDateBeginning = new DateTimeOffset(asOfDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return await _dbContext.Contracts
            .Where(c => c.Status == ContractStatus.Active
                     && c.EndDate >= asOfDate
                     && c.EndDate <= maxDate
                     && (c.LastExpirationReminderSentAt == null || c.LastExpirationReminderSentAt.Value < asOfDateBeginning))
            .OrderBy(c => c.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        await _dbContext.Contracts.AddAsync(contract, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
