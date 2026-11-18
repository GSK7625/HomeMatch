using System.Security.Claims;
using HomeMatch.Modules.Billing.Application.Commands.CancelInvoice;
using HomeMatch.Modules.Billing.Application.Commands.CreateInvoice;
using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Queries.GetInvoiceById;
using HomeMatch.Modules.Billing.Application.Queries.GetInvoices;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Billing.Presentation.Endpoints;

public static class InvoiceEndpoints
{
    public static RouteGroupBuilder MapInvoiceEndpoints(this RouteGroupBuilder group)
    {
        var invoices = group.MapGroup("/invoices").RequireAuthorization();

        // 1. Tạo hóa đơn tiền phòng định kỳ mới (Landlord)
        invoices.MapPost("", async (
            [FromBody] CreateInvoiceRequest request,
            [FromServices] CreateInvoiceHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<InvoiceResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new CreateInvoiceCommand(
                request.ContractId,
                request.BillingMonth,
                request.BillingYear,
                userId,
                IsAdmin(user));

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<InvoiceResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Created($"/api/v1/billing/invoices/{response.Id}", ApiResponse.Success(response));
        })
        .WithName("CreateInvoice")
        .WithSummary("Chủ nhà tạo hóa đơn tiền thuê phòng cho tháng hiện tại hoặc tháng cuối hợp đồng");

        // 2. Tra cứu danh sách hóa đơn phân trang với bộ lọc (Landlord / Tenant / Admin)
        invoices.MapGet("", async (
            [FromQuery] long? contractId,
            [FromQuery] long? propertyId,
            [FromQuery] string? status,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] GetInvoicesHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedInvoicesResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var filter = new InvoiceFilter(
                ContractId: contractId,
                PropertyId: propertyId,
                Status: status,
                BillingMonth: month,
                BillingYear: year);

            var query = new GetInvoicesQuery(
                filter,
                page ?? 1,
                pageSize ?? 20,
                userId,
                IsLandlord(user),
                IsTenant(user),
                IsAdmin(user));

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<PagedInvoicesResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("GetInvoices")
        .WithSummary("Tra cứu danh sách hóa đơn phân trang theo bộ lọc và vai trò");

        // 3. Tra cứu chi tiết một hóa đơn (Landlord / Tenant / Admin)
        invoices.MapGet("/{id:long}", async (
            [FromRoute] long id,
            [FromServices] GetInvoiceByIdHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<InvoiceDetailResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetInvoiceByIdQuery(id, userId, IsAdmin(user));
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<InvoiceDetailResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("GetInvoiceById")
        .WithSummary("Xem chi tiết hóa đơn tiền phòng và lịch sử giao dịch thanh toán");

        // 4. Hủy hóa đơn tiền phòng (Landlord)
        invoices.MapPost("/{id:long}/cancel", async (
            [FromRoute] long id,
            [FromBody] CancelInvoiceRequest request,
            [FromServices] CancelInvoiceHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<InvoiceResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new CancelInvoiceCommand(
                id,
                request.Reason,
                userId,
                IsAdmin(user));

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<InvoiceResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("CancelInvoice")
        .WithSummary("Chủ nhà hủy hóa đơn chưa thanh toán và không có giao dịch PENDING");

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

    private static IResult ToErrorResult<T>(Error error)
    {
        int statusCode = error.Code switch
        {
            "Billing.AuthenticationRequired" => StatusCodes.Status401Unauthorized,
            "Billing.Forbidden" => StatusCodes.Status403Forbidden,
            "Billing.InvoiceNotFound" => StatusCodes.Status404NotFound,
            "Billing.ContractNotFound" => StatusCodes.Status404NotFound,
            "Billing.DuplicatePeriodInvoice" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(ApiResponse.Failure<T>(error), statusCode: statusCode);
    }
}
