using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Domain.Enums;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Billing.Application.Queries.GetDebtInvoices;

public class GetDebtInvoicesHandler(IBillingDbContext dbContext)
{
    public async Task<Result<PagedDebtInvoicesDto>> HandleAsync(
        GetDebtInvoicesQuery query,
        CancellationToken cancellationToken = default)
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
            return Result.Failure<PagedDebtInvoicesDto>(BillingErrors.Forbidden);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Lọc theo nhóm tuổi nợ nếu được yêu cầu
        if (!string.IsNullOrWhiteSpace(query.AgingGroup))
        {
            switch (query.AgingGroup.Trim().ToLowerInvariant())
            {
                case "notdue":
                    invoicesQuery = invoicesQuery.Where(i => i.DueDate >= today);
                    break;
                case "overdue1to30":
                    var max1To30 = today.AddDays(-1);
                    var min1To30 = today.AddDays(-30);
                    invoicesQuery = invoicesQuery.Where(i => i.DueDate <= max1To30 && i.DueDate >= min1To30);
                    break;
                case "overdue31to60":
                    var max31To60 = today.AddDays(-31);
                    var min31To60 = today.AddDays(-60);
                    invoicesQuery = invoicesQuery.Where(i => i.DueDate <= max31To60 && i.DueDate >= min31To60);
                    break;
                case "overdueover60":
                    var maxOver60 = today.AddDays(-61);
                    invoicesQuery = invoicesQuery.Where(i => i.DueDate <= maxOver60);
                    break;
            }
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : (query.PageSize > 100 ? 100 : query.PageSize);

        var totalCount = await invoicesQuery.CountAsync(cancellationToken);

        var items = await invoicesQuery
            .OrderBy(i => i.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(i =>
        {
            var diff = today.DayNumber - i.DueDate.DayNumber;
            var daysOverdue = diff > 0 ? diff : 0;
            var agingGroup = daysOverdue switch
            {
                0 => "NotDue",
                <= 30 => "Overdue1To30",
                <= 60 => "Overdue31To60",
                _ => "OverdueOver60"
            };

            return new DebtInvoiceDto(
                i.Id,
                i.InvoiceCode,
                i.ContractId,
                i.LandlordId,
                i.TenantId,
                i.RoomId,
                i.PropertyId,
                i.BillingMonth,
                i.BillingYear,
                i.TotalAmount,
                i.DueDate,
                daysOverdue,
                agingGroup,
                i.Status,
                i.CreatedAt);
        }).ToList();

        var result = new PagedDebtInvoicesDto(dtos, page, pageSize, totalCount);
        return Result.Success(result);
    }
}
