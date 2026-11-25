using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Infrastructure.Repositories;

public class UtilityReadingRepository(BillingDbContext context) : IUtilityReadingRepository
{
    public async Task<UtilityReading?> GetByRoomAndPeriodAsync(
        long roomId,
        int billingMonth,
        int billingYear,
        CancellationToken cancellationToken = default)
    {
        return await context.UtilityReadings
            .FirstOrDefaultAsync(
                u => u.RoomId == roomId && u.BillingMonth == billingMonth && u.BillingYear == billingYear,
                cancellationToken);
    }

    public async Task<UtilityReading?> GetByContractAndPeriodAsync(
        long contractId,
        int billingMonth,
        int billingYear,
        CancellationToken cancellationToken = default)
    {
        return await context.UtilityReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.ContractId == contractId && u.BillingMonth == billingMonth && u.BillingYear == billingYear,
                cancellationToken);
    }

    public async Task<UtilityReading?> GetLatestBeforePeriodAsync(
        long roomId,
        int billingMonth,
        int billingYear,
        CancellationToken cancellationToken = default)
    {
        return await context.UtilityReadings
            .AsNoTracking()
            .Where(u => u.RoomId == roomId && (u.BillingYear < billingYear || (u.BillingYear == billingYear && u.BillingMonth < billingMonth)))
            .OrderByDescending(u => u.BillingYear)
            .ThenByDescending(u => u.BillingMonth)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UtilityReading>> GetListAsync(
        UtilityReadingFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.UtilityReadings.AsNoTracking().AsQueryable();

        if (filter.PropertyId.HasValue)
        {
            query = query.Where(u => u.PropertyId == filter.PropertyId.Value);
        }

        if (filter.RoomId.HasValue)
        {
            query = query.Where(u => u.RoomId == filter.RoomId.Value);
        }

        if (filter.ContractId.HasValue)
        {
            query = query.Where(u => u.ContractId == filter.ContractId.Value);
        }

        if (filter.LandlordId.HasValue)
        {
            query = query.Where(u => u.LandlordId == filter.LandlordId.Value);
        }

        if (filter.TenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == filter.TenantId.Value);
        }

        if (filter.BillingMonth.HasValue)
        {
            query = query.Where(u => u.BillingMonth == filter.BillingMonth.Value);
        }

        if (filter.BillingYear.HasValue)
        {
            query = query.Where(u => u.BillingYear == filter.BillingYear.Value);
        }

        return await query
            .OrderByDescending(u => u.BillingYear)
            .ThenByDescending(u => u.BillingMonth)
            .ThenBy(u => u.RoomId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UtilityReading reading, CancellationToken cancellationToken = default)
    {
        await context.UtilityReadings.AddAsync(reading, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UtilityReading reading, CancellationToken cancellationToken = default)
    {
        context.UtilityReadings.Update(reading);
        await context.SaveChangesAsync(cancellationToken);
    }
}
