using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Entities;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Infrastructure.Repositories;

public class PaymentRepository(BillingDbContext context) : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByCodeAsync(string paymentCode, CancellationToken cancellationToken = default)
    {
        return await context.Payments.FirstOrDefaultAsync(p => p.PaymentCode == paymentCode, cancellationToken);
    }

    public async Task<bool> HasPendingPaymentAsync(long invoiceId, CancellationToken cancellationToken = default)
    {
        return await context.Payments.AnyAsync(
            p => p.InvoiceId == invoiceId && p.Status == PaymentStatus.Pending,
            cancellationToken);
    }

    public async Task<Payment?> GetPendingPaymentByInvoiceIdAsync(long invoiceId, CancellationToken cancellationToken = default)
    {
        return await context.Payments.FirstOrDefaultAsync(
            p => p.InvoiceId == invoiceId && p.Status == PaymentStatus.Pending,
            cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await context.Payments.AddAsync(payment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        context.Payments.Update(payment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
