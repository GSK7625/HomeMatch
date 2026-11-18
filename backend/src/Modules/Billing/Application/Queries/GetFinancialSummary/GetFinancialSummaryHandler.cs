using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Leasing;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Application.Queries.GetFinancialSummary;

public class GetFinancialSummaryHandler(
    IBillingDbContext dbContext,
    ILeasingContractReader leasingContractReader)
{
    public async Task<Result<FinancialSummaryReportDto>> HandleAsync(
        GetFinancialSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!query.IsAdmin && !query.IsLandlord)
        {
            return Result.Failure<FinancialSummaryReportDto>(BillingErrors.Forbidden);
        }

        // 1. Khoản phải thu (Invoices không bị hủy)
        var invoicesQuery = dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled);

        if (!query.IsAdmin)
        {
            invoicesQuery = invoicesQuery.Where(i => i.LandlordId == query.UserId);
        }

        if (query.PropertyId.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.PropertyId == query.PropertyId.Value);
        }

        if (query.FromDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.CreatedAt <= query.ToDate.Value);
        }

        var invoices = await invoicesQuery.ToListAsync(cancellationToken);
        var totalReceivable = invoices.Sum(i => i.TotalAmount);

        // 2. Khoản thực thu (Ghi nhận theo ngày thanh toán thành công - BR-BIL-28)
        var paymentsQuery = dbContext.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
            .Where(p => p.Status == PaymentStatus.Success && p.Invoice != null);

        if (!query.IsAdmin)
        {
            paymentsQuery = paymentsQuery.Where(p => p.Invoice!.LandlordId == query.UserId);
        }

        if (query.PropertyId.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.Invoice!.PropertyId == query.PropertyId.Value);
        }

        if (query.FromDate.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.CompletedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.CompletedAt <= query.ToDate.Value);
        }

        var totalCollected = await paymentsQuery.SumAsync(p => p.Amount, cancellationToken);

        // 3. Khoản nợ còn lại & số lượng
        var totalOutstanding = invoices
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue)
            .Sum(i => i.TotalAmount);

        var totalInvoicesCount = invoices.Count;
        var paidInvoicesCount = invoices.Count(i => i.Status == InvoiceStatus.Paid);
        var unpaidInvoicesCount = invoices.Count(i => i.Status == InvoiceStatus.Unpaid);
        var overdueInvoicesCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue);

        var collectionRate = totalReceivable > 0
            ? Math.Round((double)totalCollected / totalReceivable * 100, 2)
            : 0.0;

        // 4. Lấy tiền cọc từ Leasing nếu là chủ nhà (BR-BIL-31)
        long totalDeposit = 0;
        if (query.IsLandlord)
        {
            totalDeposit = await leasingContractReader.GetTotalDepositAmountAsync(query.UserId, cancellationToken);
        }

        var report = new FinancialSummaryReportDto(
            TotalReceivable: totalReceivable,
            TotalCollected: totalCollected,
            TotalOutstanding: totalOutstanding,
            TotalDepositHolding: totalDeposit,
            TotalInvoicesCount: totalInvoicesCount,
            PaidInvoicesCount: paidInvoicesCount,
            UnpaidInvoicesCount: unpaidInvoicesCount,
            OverdueInvoicesCount: overdueInvoicesCount,
            CollectionRate: collectionRate);

        return Result.Success(report);
    }
}
