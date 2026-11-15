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
