using System.Security.Claims;
using HomeMatch.Modules.Billing.Application.Commands.ProcessInvoiceOverdue;
using HomeMatch.Modules.Billing.Application.Queries.GetDebtInvoices;
using HomeMatch.Modules.Billing.Application.Queries.GetDebtSummary;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Billing.Presentation.Endpoints;

public static class DebtEndpoints
{
    public static RouteGroupBuilder MapDebtEndpoints(this RouteGroupBuilder group)
    {
        var debts = group.MapGroup("/debts").RequireAuthorization();

        // 1. Thống kê tổng hợp công nợ và nhóm tuổi nợ (Landlord / Tenant / Admin)
        debts.MapGet("/summary", async (
            [FromQuery] long? propertyId,
            [FromQuery] long? contractId,
            [FromServices] GetDebtSummaryHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DebtSummaryResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDebtSummaryQuery(
                PropertyId: propertyId,
                ContractId: contractId,
                UserId: userId,
                IsLandlord: IsLandlord(user),
                IsTenant: IsTenant(user),
                IsAdmin: IsAdmin(user));

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return Results.Json(ApiResponse.Failure<DebtSummaryResponse>(result.Error), statusCode: StatusCodes.Status400BadRequest);
            }

            var response = new DebtSummaryResponse(
                TotalUnpaidDebt: result.Value.TotalUnpaidDebt,
                TotalOverdueDebt: result.Value.TotalOverdueDebt,
                TotalUnpaidInvoices: result.Value.TotalUnpaidInvoices,
                TotalOverdueInvoices: result.Value.TotalOverdueInvoices,
                AgingGroups: result.Value.AgingGroups.Select(g => new DebtAgingGroupResponse(
                    g.GroupCode,
                    g.GroupName,
                    g.InvoiceCount,
                    g.TotalAmount)).ToList());

            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("GetDebtSummary")
        .WithSummary("Thống kê tổng hợp công nợ và 4 nhóm tuổi nợ (chưa đến hạn, 1-30, 31-60, trên 60 ngày)");

        // 2. Tra cứu danh sách hóa đơn còn nợ phân trang (Landlord / Tenant / Admin)
        debts.MapGet("/invoices", async (
            [FromQuery] long? propertyId,
            [FromQuery] long? contractId,
            [FromQuery] string? agingGroup,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] GetDebtInvoicesHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedDebtInvoicesResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDebtInvoicesQuery(
                PropertyId: propertyId,
                ContractId: contractId,
                AgingGroup: agingGroup,
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                UserId: userId,
                IsLandlord: IsLandlord(user),
                IsTenant: IsTenant(user),
                IsAdmin: IsAdmin(user));

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return Results.Json(ApiResponse.Failure<PagedDebtInvoicesResponse>(result.Error), statusCode: StatusCodes.Status400BadRequest);
            }

            var responseItems = result.Value.Items.Select(i => new DebtInvoiceResponse(
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
                i.DaysOverdue,
                i.AgingGroup,
                i.Status,
                i.CreatedAt)).ToList();

            var response = new PagedDebtInvoicesResponse(
                Items: responseItems,
                Page: result.Value.Page,
                PageSize: result.Value.PageSize,
                TotalCount: result.Value.TotalCount);

            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("GetDebtInvoices")
        .WithSummary("Tra cứu danh sách hóa đơn còn nợ (UNPAID/OVERDUE) phân trang có lọc theo tòa nhà và nhóm tuổi nợ");

        // 3. Kích hoạt thủ công quá trình quét quá hạn hóa đơn và hết hạn giao dịch (Admin only)
        debts.MapPost("/process-overdue", async (
            [FromServices] ProcessInvoiceOverdueHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!IsAdmin(user))
            {
                return Results.Json(
                    ApiResponse.Failure<ProcessInvoiceOverdueResult>(BillingErrors.Forbidden),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await handler.HandleAsync(new ProcessInvoiceOverdueCommand(), cancellationToken);
            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("ProcessOverdueInvoices")
        .WithSummary("Quản trị viên kích hoạt thủ công quét quá hạn hóa đơn và giải phóng giao dịch PENDING hết hạn");

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out long userId)
    {
        var id = RequestActor.UserId(user);
        if (id.HasValue)
        {
            userId = id.Value;
            return true;
        }

        userId = 0;
        return false;
    }

    private static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("ADMIN");

    private static bool IsLandlord(ClaimsPrincipal user) =>
        user.IsInRole("Landlord") || user.IsInRole("LANDLORD");

    private static bool IsTenant(ClaimsPrincipal user) =>
        user.IsInRole("Tenant") || user.IsInRole("TENANT");
}
