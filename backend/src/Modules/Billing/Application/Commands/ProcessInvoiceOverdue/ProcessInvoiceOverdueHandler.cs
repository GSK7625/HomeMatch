using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Billing.Application.Commands.ProcessInvoiceOverdue;

public class ProcessInvoiceOverdueHandler(
    IBillingDbContext dbContext,
    ILogger<ProcessInvoiceOverdueHandler> logger)
{
    public async Task<Result<ProcessInvoiceOverdueResult>> HandleAsync(
        ProcessInvoiceOverdueCommand command,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        // 1. Quét các hóa đơn UNPAID đã quá hạn thanh toán -> OVERDUE (BR-BIL-26)
        var overdueInvoices = await dbContext.Invoices
            .Where(i => i.Status == InvoiceStatus.Unpaid && i.DueDate < today)
            .ToListAsync(cancellationToken);

        foreach (var invoice in overdueInvoices)
        {
            invoice.Status = InvoiceStatus.Overdue;
            invoice.UpdatedAt = now;
        }

        // 2. Quét các giao dịch thanh toán PENDING đã quá thời hạn hiệu lực -> EXPIRED (BR-BIL-22)
        var expiredPayments = await dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Pending && p.ExpiredAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var payment in expiredPayments)
        {
            payment.Status = PaymentStatus.Expired;
        }

        if (overdueInvoices.Count > 0 || expiredPayments.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Đã cập nhật tự động: {OverdueCount} hóa đơn quá hạn -> OVERDUE, {ExpiredCount} giao dịch hết hạn -> EXPIRED.",
                overdueInvoices.Count,
                expiredPayments.Count);
        }

        return Result.Success(new ProcessInvoiceOverdueResult(
            OverdueInvoicesCount: overdueInvoices.Count,
            ExpiredPaymentsCount: expiredPayments.Count));
    }
}
