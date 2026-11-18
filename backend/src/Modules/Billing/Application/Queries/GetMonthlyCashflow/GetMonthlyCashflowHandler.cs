using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Application.Queries.GetMonthlyCashflow;

public class GetMonthlyCashflowHandler(IBillingDbContext dbContext)
{
    public async Task<Result<IReadOnlyList<MonthlyCashflowDto>>> HandleAsync(
        GetMonthlyCashflowQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!query.IsAdmin && !query.IsLandlord)
        {
            return Result.Failure<IReadOnlyList<MonthlyCashflowDto>>(BillingErrors.Forbidden);
        }

        var targetYear = query.Year ?? DateTime.UtcNow.Year;

        // 1. Tiền lập hóa đơn theo kỳ tháng (BillingYear == targetYear)
        var invoicesQuery = dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BillingYear == targetYear && i.Status != InvoiceStatus.Cancelled);

        if (!query.IsAdmin)
        {
            invoicesQuery = invoicesQuery.Where(i => i.LandlordId == query.UserId);
        }

        if (query.PropertyId.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.PropertyId == query.PropertyId.Value);
        }

        var invoices = await invoicesQuery.ToListAsync(cancellationToken);

        // 2. Tiền thực thu trong từng tháng theo ngày hoàn tất giao dịch (BR-BIL-28)
        var paymentsQuery = dbContext.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
            .Where(p => p.Status == PaymentStatus.Success
                     && p.CompletedAt.HasValue
                     && p.CompletedAt.Value.Year == targetYear
                     && p.Invoice != null);

        if (!query.IsAdmin)
        {
            paymentsQuery = paymentsQuery.Where(p => p.Invoice!.LandlordId == query.UserId);
        }

        if (query.PropertyId.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.Invoice!.PropertyId == query.PropertyId.Value);
        }

        var payments = await paymentsQuery.ToListAsync(cancellationToken);

        // 3. Tổng hợp 12 tháng
        var result = new List<MonthlyCashflowDto>();
        for (int month = 1; month <= 12; month++)
        {
            var monthInvoices = invoices.Where(i => i.BillingMonth == month).ToList();
            var billedAmount = monthInvoices.Sum(i => i.TotalAmount);
            var collectedInMonth = payments
                .Where(p => p.CompletedAt!.Value.Month == month)
                .Sum(p => p.Amount);

            var outstandingAmount = monthInvoices
                .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.TotalAmount);

            result.Add(new MonthlyCashflowDto(
                Year: targetYear,
                Month: month,
                BilledAmount: billedAmount,
                CollectedAmount: collectedInMonth,
                OutstandingAmount: outstandingAmount));
        }

        return Result.Success<IReadOnlyList<MonthlyCashflowDto>>(result);
    }
}
