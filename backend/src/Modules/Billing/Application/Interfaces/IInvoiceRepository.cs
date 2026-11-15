using HomeMatch.Modules.Billing.Domain.Entities;

namespace HomeMatch.Modules.Billing.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdWithPaymentsAsync(long id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByCodeAsync(string invoiceCode, CancellationToken cancellationToken = default);
    Task<bool> HasActiveInvoiceForPeriodAsync(long contractId, int billingMonth, int billingYear, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
