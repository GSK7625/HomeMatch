using HomeMatch.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Application.Interfaces;

public interface IBillingDbContext
{
    DbSet<Invoice> Invoices { get; }
    DbSet<Payment> Payments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
