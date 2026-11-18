using HomeMatch.Modules.Billing.Domain.Entities;

namespace HomeMatch.Modules.Billing.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByCodeAsync(string paymentCode, CancellationToken cancellationToken = default);
    Task<bool> HasPendingPaymentAsync(long invoiceId, CancellationToken cancellationToken = default);
    Task<Payment?> GetPendingPaymentByInvoiceIdAsync(long invoiceId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}
