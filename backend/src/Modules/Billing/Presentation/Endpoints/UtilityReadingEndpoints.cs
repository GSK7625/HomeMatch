using System.Security.Claims;
using HomeMatch.Modules.Billing.Application.Commands.RecordUtilityReading;
using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Queries.GetUtilityReadings;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Billing.Presentation.Endpoints;

public static class UtilityReadingEndpoints
{
    public static RouteGroupBuilder MapUtilityReadingEndpoints(this RouteGroupBuilder group)
    {
        var readings = group.MapGroup("/utility-readings").RequireAuthorization();

        // 1. Ghi hoặc cập nhật chỉ số công tơ điện nước theo tháng và tự động tính tiền điện nước (Landlord / Admin)
        readings.MapPost("", async (
            [FromBody] RecordUtilityReadingRequest request,
            [FromServices] RecordUtilityReadingHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<UtilityReadingResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (!IsLandlord(user) && !IsAdmin(user))
            {
                return Results.Json(
                    ApiResponse.Failure<UtilityReadingResponse>(BillingErrors.Forbidden),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var command = new RecordUtilityReadingCommand(
                request.ContractId,
                request.BillingMonth,
                request.BillingYear,
                request.ElectricityOld,
                request.ElectricityNew,
                request.WaterOld,
                request.WaterNew,
                request.ElectricityUnitPriceOverride,
                request.WaterUnitPriceOverride,
                request.ServiceCostOverride,
                request.ReadingDate,
                userId,
                IsAdmin(user));

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<UtilityReadingResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("RecordUtilityReading")
        .WithSummary("Chủ nhà ghi chỉ số điện nước đầu kỳ và cuối kỳ theo tháng, hệ thống tự động tính tiền điện nước theo đơn giá");

        // 2. Tra cứu danh sách chỉ số tiêu thụ và bảng chi phí sử dụng điện nước (Landlord / Tenant / Admin)
        readings.MapGet("", async (
            [FromQuery] long? propertyId,
            [FromQuery] long? roomId,
            [FromQuery] long? contractId,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromServices] GetUtilityReadingsHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<IReadOnlyList<UtilityReadingResponse>>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var filter = new UtilityReadingFilter(
                PropertyId: propertyId,
                RoomId: roomId,
                ContractId: contractId,
                BillingMonth: month,
                BillingYear: year);

            var query = new GetUtilityReadingsQuery(
                filter,
                userId,
                IsLandlord(user),
                IsTenant(user),
                IsAdmin(user));

            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<IReadOnlyList<UtilityReadingResponse>>(result.Error);
            }

            var response = result.Value.Select(InvoiceResponseMapper.ToResponse).ToList();
            return Results.Ok(ApiResponse.Success<IReadOnlyList<UtilityReadingResponse>>(response));
        })
        .WithName("GetUtilityReadings")
        .WithSummary("Tra cứu danh sách chỉ số tiêu thụ và bảng chi phí sử dụng điện nước theo kỳ cho Chủ nhà và Người thuê");

        // 3. Lấy chỉ số cuối kỳ gần nhất của phòng để gợi ý chỉ số đầu kỳ cho kỳ mới (Landlord / Admin)
        readings.MapGet("/latest-previous", async (
            [FromQuery] long roomId,
            [FromQuery] int month,
            [FromQuery] int year,
            [FromServices] IUtilityReadingRepository readingRepository,
            [FromServices] IRoomListingReader roomListingReader,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<UtilityReadingResponse?>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (!IsLandlord(user) && !IsAdmin(user))
            {
                return Results.Json(
                    ApiResponse.Failure<UtilityReadingResponse?>(BillingErrors.Forbidden),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            if (month < 1 || month > 12 || year < 2020)
            {
                return Results.Json(
                    ApiResponse.Failure<UtilityReadingResponse?>(new Error("Billing.InvalidPeriod", "Kỳ tháng/năm không hợp lệ.")),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var room = await roomListingReader.GetRoomAsync(roomId, cancellationToken);
            if (room is null)
            {
                return Results.Json(
                    ApiResponse.Failure<UtilityReadingResponse?>(new Error("Billing.RoomNotFound", "Không tìm thấy thông tin phòng yêu cầu.")),
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (!IsAdmin(user) && room.LandlordId != userId)
            {
                return Results.Json(
                    ApiResponse.Failure<UtilityReadingResponse?>(BillingErrors.Forbidden),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var latest = await readingRepository.GetLatestBeforePeriodAsync(roomId, month, year, cancellationToken);
            var response = latest is not null
                ? InvoiceResponseMapper.ToResponse(RecordUtilityReadingHandler.ToDto(latest))
                : null;

            return Results.Ok(ApiResponse.Success(response));
        })
        .WithName("GetLatestPreviousUtilityReading")
        .WithSummary("Lấy chỉ số điện nước cuối kỳ của tháng gần nhất trước đó để tự động điền chỉ số đầu kỳ");

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
            "Billing.ContractNotFound" => StatusCodes.Status404NotFound,
            "Billing.CannotModifyReadingWithActiveInvoice" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(ApiResponse.Failure<T>(error), statusCode: statusCode);
    }
}
