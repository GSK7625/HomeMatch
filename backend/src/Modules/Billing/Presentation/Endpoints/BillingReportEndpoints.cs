using System.Security.Claims;
using HomeMatch.Modules.Billing.Application.Queries.GetFinancialSummary;
using HomeMatch.Modules.Billing.Application.Queries.GetMonthlyCashflow;
using HomeMatch.Modules.Billing.Application.Queries.GetPropertyFinancialReport;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Billing.Presentation.Endpoints;

public static class BillingReportEndpoints
{
    public static RouteGroupBuilder MapBillingReportEndpoints(this RouteGroupBuilder group)
    {
        var reports = group.MapGroup("/reports").RequireAuthorization();

        // 1. Báo cáo tài chính tổng quan (Landlord / Admin)
        reports.MapGet("/summary", async (
            [FromQuery] long? propertyId,
            [FromQuery] DateTimeOffset? fromDate,
            [FromQuery] DateTimeOffset? toDate,
            [FromServices] GetFinancialSummaryHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<FinancialSummaryResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetFinancialSummaryQuery(
                PropertyId: propertyId,
                FromDate: fromDate,
                ToDate: toDate,
                UserId: userId,
                IsLandlord: IsLandlord(user),
                IsAdmin: IsAdmin(user));

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return Results.Json(ApiResponse.Failure<FinancialSummaryResponse>(result.Error), statusCode: StatusCodes.Status403Forbidden);
            }

            var response = new FinancialSummaryResponse(
                TotalReceivable: result.Value.TotalReceivable,
                TotalCollected: result.Value.TotalCollected,
                TotalOutstanding: result.Value.TotalOutstanding,
                TotalDepositHolding: result.Value.TotalDepositHolding,
                TotalInvoicesCount: result.Value.TotalInvoicesCount,
                PaidInvoicesCount: result.Value.PaidInvoicesCount,
                UnpaidInvoicesCount: result.Value.UnpaidInvoicesCount,
                OverdueInvoicesCount: result.Value.OverdueInvoicesCount,
                CollectionRate: result.Value.CollectionRate);

            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("GetBillingFinancialSummary")
        .WithSummary("Báo cáo tài chính tổng quan: khoản phải thu, thực thu theo ngày hoàn tất (BR-BIL-28), nợ đọng và tiền cọc từ Leasing (BR-BIL-31)");

        // 2. Báo cáo phân bổ theo từng tòa nhà (Landlord / Admin)
        reports.MapGet("/properties", async (
            [FromQuery] DateTimeOffset? fromDate,
            [FromQuery] DateTimeOffset? toDate,
            [FromServices] GetPropertyFinancialReportHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<IReadOnlyList<PropertyFinancialResponse>>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetPropertyFinancialReportQuery(
                FromDate: fromDate,
                ToDate: toDate,
                UserId: userId,
                IsLandlord: IsLandlord(user),
                IsAdmin: IsAdmin(user));

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return Results.Json(ApiResponse.Failure<IReadOnlyList<PropertyFinancialResponse>>(result.Error), statusCode: StatusCodes.Status403Forbidden);
            }

            var responses = result.Value.Select(p => new PropertyFinancialResponse(
                PropertyId: p.PropertyId,
                TotalReceivable: p.TotalReceivable,
                TotalCollected: p.TotalCollected,
                TotalOutstanding: p.TotalOutstanding,
                InvoicesCount: p.InvoicesCount,
                CollectionRate: p.CollectionRate)).ToList();

            return Results.Ok(ApiResponse.Success(responses));
        })
        .WithName("GetBillingPropertiesReport")
        .WithSummary("Báo cáo tài chính chi tiết theo từng tòa nhà (BR-BIL-30)");

        // 3. Báo cáo dòng tiền 12 tháng (Landlord / Admin)
        reports.MapGet("/cashflow", async (
            [FromQuery] long? propertyId,
            [FromQuery] int? year,
            [FromServices] GetMonthlyCashflowHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<IReadOnlyList<MonthlyCashflowResponse>>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetMonthlyCashflowQuery(
                PropertyId: propertyId,
                Year: year,
                UserId: userId,
                IsLandlord: IsLandlord(user),
                IsAdmin: IsAdmin(user));

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return Results.Json(ApiResponse.Failure<IReadOnlyList<MonthlyCashflowResponse>>(result.Error), statusCode: StatusCodes.Status403Forbidden);
            }

            var responses = result.Value.Select(m => new MonthlyCashflowResponse(
                Year: m.Year,
                Month: m.Month,
                BilledAmount: m.BilledAmount,
                CollectedAmount: m.CollectedAmount,
                OutstandingAmount: m.OutstandingAmount)).ToList();

            return Results.Ok(ApiResponse.Success(responses));
        })
        .WithName("GetBillingMonthlyCashflow")
        .WithSummary("Báo cáo so sánh dòng tiền lập hóa đơn và thực thu 12 tháng trong năm");

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
}
