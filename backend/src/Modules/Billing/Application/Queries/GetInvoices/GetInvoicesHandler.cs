using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Application.Queries.GetInvoices;

public class GetInvoicesHandler
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoicesHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<PagedInvoicesDto>> HandleAsync(GetInvoicesQuery query, CancellationToken cancellationToken = default)
    {
        var filter = query.Filter;

        // Ràng buộc bảo mật dữ liệu theo vai trò (Tenant chỉ xem hóa đơn của mình, Landlord chỉ xem hóa đơn mình quản lý)
        if (!query.IsAdmin)
        {
            if (query.IsTenant)
            {
                filter = filter with { TenantId = query.RequestUserId };
            }
            else if (query.IsLandlord)
            {
                filter = filter with { LandlordId = query.RequestUserId };
            }
        }

        var (items, totalCount) = await _invoiceRepository.GetPagedAsync(filter, query.Page, query.PageSize, cancellationToken);

        var dtos = items.Select(i => new InvoiceDto(
            i.Id,
            i.InvoiceCode,
            i.ContractId,
            i.LandlordId,
            i.TenantId,
            i.RoomId,
            i.PropertyId,
            i.BillingMonth,
            i.BillingYear,
            i.RentAmount,
            i.ElectricityAmount,
            i.WaterAmount,
            i.ServiceAmount,
            i.DiscountAmount,
            i.TotalAmount,
            i.PaidAmount,
            i.DueDate,
            i.PaidAt,
            i.Status,
            i.CancelReason,
            i.CancelledAt,
            i.CreatedAt,
            i.UpdatedAt)).ToList();

        var pagedDto = new PagedInvoicesDto(
            dtos,
            query.Page > 0 ? query.Page : 1,
            query.PageSize > 0 ? query.PageSize : 20,
            totalCount);

        return Result.Success(pagedDto);
    }
}
