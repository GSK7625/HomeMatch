using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Application.Queries.GetDebtSummary;

public class GetDebtSummaryHandler(IBillingDbContext dbContext)
{
    public async Task<Result<DebtSummaryDto>> HandleAsync(GetDebtSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var invoicesQuery = dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue);

        // Phân quyền dữ liệu
        if (query.IsAdmin)
        {
            if (query.PropertyId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.PropertyId == query.PropertyId.Value);
            }

            if (query.ContractId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.ContractId == query.ContractId.Value);
            }
        }
        else if (query.IsLandlord)
        {
            invoicesQuery = invoicesQuery.Where(i => i.LandlordId == query.UserId);

            if (query.PropertyId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.PropertyId == query.PropertyId.Value);
            }

            if (query.ContractId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.ContractId == query.ContractId.Value);
            }
        }
        else if (query.IsTenant)
        {
            invoicesQuery = invoicesQuery.Where(i => i.TenantId == query.UserId);

            if (query.ContractId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.ContractId == query.ContractId.Value);
            }
        }
        else
        {
            return Result.Failure<DebtSummaryDto>(BillingErrors.Forbidden);
        }

        var invoices = await invoicesQuery.ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Tổng công nợ & hóa đơn
        var totalUnpaidDebt = invoices.Sum(i => i.TotalAmount);
        var overdueInvoices = invoices.Where(i => i.Status == InvoiceStatus.Overdue || i.DueDate < today).ToList();
        var totalOverdueDebt = overdueInvoices.Sum(i => i.TotalAmount);

        // 2. Phân nhóm tuổi nợ (Mục 6.4 BRD)
        var notDue = invoices.Where(i => today <= i.DueDate).ToList();
        var overdue1To30 = invoices.Where(i =>
        {
            var diff = today.DayNumber - i.DueDate.DayNumber;
            return diff is >= 1 and <= 30;
        }).ToList();

        var overdue31To60 = invoices.Where(i =>
        {
            var diff = today.DayNumber - i.DueDate.DayNumber;
            return diff is >= 31 and <= 60;
        }).ToList();

        var overdueOver60 = invoices.Where(i =>
        {
            var diff = today.DayNumber - i.DueDate.DayNumber;
            return diff > 60;
        }).ToList();

        var agingGroups = new List<DebtAgingGroupDto>
        {
            new("NotDue", "Chưa đến hạn", notDue.Count, notDue.Sum(i => i.TotalAmount)),
            new("Overdue1To30", "Quá hạn 1–30 ngày", overdue1To30.Count, overdue1To30.Sum(i => i.TotalAmount)),
            new("Overdue31To60", "Quá hạn 31–60 ngày", overdue31To60.Count, overdue31To60.Sum(i => i.TotalAmount)),
            new("OverdueOver60", "Quá hạn trên 60 ngày", overdueOver60.Count, overdueOver60.Sum(i => i.TotalAmount))
        };

        var summary = new DebtSummaryDto(
            TotalUnpaidDebt: totalUnpaidDebt,
            TotalOverdueDebt: totalOverdueDebt,
            TotalUnpaidInvoices: invoices.Count,
            TotalOverdueInvoices: overdueInvoices.Count,
            AgingGroups: agingGroups);

        return Result.Success(summary);
    }
}
