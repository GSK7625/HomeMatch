using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Infrastructure.Repositories;

public class InvoiceRepository(BillingDbContext context) : IInvoiceRepository
{
    public async Task<Invoice?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByIdWithPaymentsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByCodeAsync(string invoiceCode, CancellationToken cancellationToken = default)
    {
        return await context.Invoices.FirstOrDefaultAsync(i => i.InvoiceCode == invoiceCode, cancellationToken);
    }

    public async Task<bool> HasActiveInvoiceForPeriodAsync(long contractId, int billingMonth, int billingYear, CancellationToken cancellationToken = default)
    {
        return await context.Invoices.AnyAsync(
            i => i.ContractId == contractId
              && i.BillingMonth == billingMonth
              && i.BillingYear == billingYear
              && i.Status != InvoiceStatus.Cancelled,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(
        InvoiceFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Invoices.AsNoTracking().AsQueryable();

        if (filter.ContractId.HasValue)
        {
            query = query.Where(i => i.ContractId == filter.ContractId.Value);
        }

        if (filter.PropertyId.HasValue)
        {
            query = query.Where(i => i.PropertyId == filter.PropertyId.Value);
        }

        if (filter.LandlordId.HasValue)
        {
            query = query.Where(i => i.LandlordId == filter.LandlordId.Value);
        }

        if (filter.TenantId.HasValue)
        {
            query = query.Where(i => i.TenantId == filter.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(i => i.Status == filter.Status);
        }

        if (filter.BillingMonth.HasValue)
        {
            query = query.Where(i => i.BillingMonth == filter.BillingMonth.Value);
        }

        if (filter.BillingYear.HasValue)
        {
            query = query.Where(i => i.BillingYear == filter.BillingYear.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? Math.Min(pageSize, 100) : 20;

        var items = await query
            .OrderByDescending(i => i.BillingYear)
            .ThenByDescending(i => i.BillingMonth)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await context.Invoices.AddAsync(invoice, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        context.Invoices.Update(invoice);
        await context.SaveChangesAsync(cancellationToken);
    }
}
