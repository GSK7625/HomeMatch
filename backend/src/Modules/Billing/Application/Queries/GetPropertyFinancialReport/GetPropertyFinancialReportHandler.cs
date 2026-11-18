using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Application.Queries.GetPropertyFinancialReport;

public class GetPropertyFinancialReportHandler(IBillingDbContext dbContext)
{
    public async Task<Result<IReadOnlyList<PropertyFinancialReportDto>>> HandleAsync(
        GetPropertyFinancialReportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!query.IsAdmin && !query.IsLandlord)
        {
            return Result.Failure<IReadOnlyList<PropertyFinancialReportDto>>(BillingErrors.Forbidden);
        }

        var invoicesQuery = dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled);

        if (!query.IsAdmin)
        {
            invoicesQuery = invoicesQuery.Where(i => i.LandlordId == query.UserId);
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

        var propertyGroups = invoices
            .GroupBy(i => i.PropertyId)
            .Select(g =>
            {
                var totalReceivable = g.Sum(i => i.TotalAmount);
                var totalCollected = g.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);
                var totalOutstanding = g.Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue).Sum(i => i.TotalAmount);
                var collectionRate = totalReceivable > 0
                    ? Math.Round((double)totalCollected / totalReceivable * 100, 2)
                    : 0.0;

                return new PropertyFinancialReportDto(
                    PropertyId: g.Key,
                    TotalReceivable: totalReceivable,
                    TotalCollected: totalCollected,
                    TotalOutstanding: totalOutstanding,
                    InvoicesCount: g.Count(),
                    CollectionRate: collectionRate);
            })
            .OrderByDescending(p => p.TotalReceivable)
            .ToList();

        return Result.Success<IReadOnlyList<PropertyFinancialReportDto>>(propertyGroups);
    }
}
