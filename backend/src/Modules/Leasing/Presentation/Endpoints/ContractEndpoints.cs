using System.Security.Claims;
using HomeMatch.Modules.Leasing.Application.Commands.ActivateContract;
using HomeMatch.Modules.Leasing.Application.Commands.CancelContract;
using HomeMatch.Modules.Leasing.Application.Commands.ConfirmHandover;
using HomeMatch.Modules.Leasing.Application.Commands.CreateContract;
using HomeMatch.Modules.Leasing.Application.Commands.RecordDepositTransaction;
using HomeMatch.Modules.Leasing.Application.Commands.RejectContract;
using HomeMatch.Modules.Leasing.Application.Commands.RequestSignatureOtp;
using HomeMatch.Modules.Leasing.Application.Commands.SendContractForSignature;
using HomeMatch.Modules.Leasing.Application.Commands.SignContract;
using HomeMatch.Modules.Leasing.Application.Commands.TerminateContract;
using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractById;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractDetails;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractDocumentUrl;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractsByLandlord;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractsByTenant;
using HomeMatch.Modules.Leasing.Application.Queries.GetDepositBalance;
using HomeMatch.Modules.Leasing.Application.Queries.GetDepositTransactions;
using HomeMatch.Modules.Leasing.Application.Queries.GetHandoverDetails;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints;

public static class ContractEndpoints
{
    public static IEndpointRouteBuilder MapContractEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contracts").RequireAuthorization();

        // 1. Tạo hợp đồng thuê mới (hỗ trợ cả Form kèm file PDF xuất từ bản preview và JSON thông thường)
        group.MapPost("", async (
            HttpRequest request,
            [FromServices] CreateContractHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            CreateContractCommand command;
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(cancellationToken);
                if (!long.TryParse(form["roomId"], out var roomId) ||
                    !long.TryParse(form["tenantId"], out var tenantId) ||
                    !DateOnly.TryParse(form["startDate"], out var startDate) ||
                    !DateOnly.TryParse(form["endDate"], out var endDate) ||
                    !long.TryParse(form["monthlyRent"], out var monthlyRent) ||
                    !long.TryParse(form["depositAmount"], out var depositAmount))
                {
                    return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                        new Error("Leasing.InvalidFormData", "Dữ liệu form tạo hợp đồng không hợp lệ hoặc thiếu trường bắt buộc.")));
                }

                int.TryParse(form["billingDueDay"], out var billingDueDay);
                if (billingDueDay <= 0) billingDueDay = 5;

                var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
                Stream? fileStream = null;
                string? fileName = null;
                string? contentType = null;
                long fileLength = 0;

                if (file is not null && file.Length > 0)
                {
                    fileStream = file.OpenReadStream();
                    fileName = file.FileName;
                    contentType = file.ContentType;
                    fileLength = file.Length;
                }

                DateTimeOffset? signatureExpiresAt = null;
                if (DateTimeOffset.TryParse(form["signatureExpiresAt"], out var exp))
                {
                    signatureExpiresAt = exp;
                }

                DateTimeOffset? handoverDueAt = null;
                if (DateTimeOffset.TryParse(form["handoverDueAt"], out var hand))
                {
                    handoverDueAt = hand;
                }

                command = new CreateContractCommand(
                    LandlordId: userId,
                    RoomId: roomId,
                    TenantId: tenantId,
                    StartDate: startDate,
                    EndDate: endDate,
                    MonthlyRent: monthlyRent,
                    DepositAmount: depositAmount,
                    BillingDueDay: billingDueDay,
                    ContractCode: form["contractCode"],
                    FileStream: fileStream,
                    FileName: fileName,
                    ContentType: contentType,
                    FileLength: fileLength,
                    DocumentSnapshot: form["documentSnapshot"],
                    SignatureExpiresAt: signatureExpiresAt,
                    HandoverDueAt: handoverDueAt);
            }
            else
            {
                var body = await request.ReadFromJsonAsync<CreateContractRequest>(cancellationToken);
                if (body is null)
                {
                    return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                        new Error("Leasing.EmptyBody", "Dữ liệu yêu cầu tạo hợp đồng không được để trống.")));
                }

                var validationErrors = body.Validate();
                if (validationErrors.Count > 0)
                {
                    return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                        Error.Validation("Leasing.ValidationFailed", "Dữ liệu tạo hợp đồng thuê không hợp lệ.", validationErrors)));
                }

                command = new CreateContractCommand(
                    LandlordId: userId,
                    RoomId: body.RoomId,
                    TenantId: body.TenantId,
                    StartDate: body.StartDate,
                    EndDate: body.EndDate,
                    MonthlyRent: body.MonthlyRent,
                    DepositAmount: body.DepositAmount,
                    BillingDueDay: body.BillingDueDay,
                    ContractCode: body.ContractCode);
            }

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{result.Value.Id}",
                ApiResponse.Success(result.Value));
        })
        .DisableAntiforgery()
        .WithName("CreateContract")
        .WithSummary("Tạo hợp đồng thuê mới (Chủ nhà nhập form + gửi kèm file PDF xuất từ bản preview)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 2. Lấy danh sách hợp đồng có phân trang - Chủ nhà hoặc Người thuê
        group.MapGet("", async (
            [FromQuery] string? status,
            [FromQuery] string? roleView,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] GetContractsByLandlordHandler landlordHandler = default!,
            [FromServices] GetContractsByTenantHandler tenantHandler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedContractsDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var effectiveRoleView = roleView;
            if (string.IsNullOrWhiteSpace(effectiveRoleView))
            {
                var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
                                ?? httpContext.User.FindFirst("role")?.Value;
                effectiveRoleView = string.Equals(roleClaim, "Tenant", StringComparison.OrdinalIgnoreCase)
                    ? "TENANT"
                    : "LANDLORD";
            }

            if (string.Equals(effectiveRoleView, "TENANT", StringComparison.OrdinalIgnoreCase))
            {
                var query = new GetContractsByTenantQuery(userId, status, page, pageSize);
                var result = await tenantHandler.HandleAsync(query, cancellationToken);
                if (result.IsFailure)
                {
                    return MapFailureResult<PagedContractsDto>(result.Error);
                }

                return Results.Ok(ApiResponse.Success(result.Value));
            }
            else
            {
                var query = new GetContractsByLandlordQuery(userId, status, page, pageSize);
                var result = await landlordHandler.HandleAsync(query, cancellationToken);
                if (result.IsFailure)
                {
                    return MapFailureResult<PagedContractsDto>(result.Error);
                }

                return Results.Ok(ApiResponse.Success(result.Value));
            }
        })
        .WithName("GetContracts")
        .WithSummary("Xem danh sách hợp đồng thuê có phân trang (Chủ nhà hoặc Người thuê)")
        .Produces<ApiResponse<PagedContractsDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PagedContractsDto>>(StatusCodes.Status401Unauthorized);

        // 3. Xem thông tin tóm tắt hợp đồng theo ID
        group.MapGet("/{id:long}", async (
            long id,
            [FromServices] GetContractByIdHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(new GetContractByIdQuery(id, userId), cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetContractById")
        .WithSummary("Xem thông tin hợp đồng thuê theo ID")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound);

        // 4. Xem chi tiết hợp đồng bao gồm tóm tắt tiền cọc
        group.MapGet("/{id:long}/details", async (
            long id,
            [FromServices] GetContractDetailsHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDetailDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(new GetContractDetailsQuery(id, userId), cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDetailDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetContractDetails")
        .WithSummary("Xem chi tiết hợp đồng thuê bao gồm tóm tắt tiền cọc")
        .Produces<ApiResponse<ContractDetailDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDetailDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDetailDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDetailDto>>(StatusCodes.Status404NotFound);

        // 5. Gửi hợp đồng cho các bên ký trực tuyến
        group.MapPost("/{id:long}/send-for-signature", async (
            long id,
            [FromBody] SendForSignatureRequest request,
            [FromServices] SendContractForSignatureHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu yêu cầu ký hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new SendContractForSignatureCommand(
                ContractId: id,
                LandlordId: userId,
                SignatureExpiresAt: request.SignatureExpiresAt,
                HandoverDueAt: request.HandoverDueAt);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("SendContractForSignature")
        .WithSummary("Gửi hợp đồng cho các bên ký trực tuyến (DRAFT -> PENDING_SIGNATURE)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 5.0.1. Lấy Presigned URL có thời hạn để xem/tải file PDF hợp đồng bảo mật
        group.MapGet("/{id:long}/document", async (
            long id,
            [FromServices] GetContractDocumentHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDocumentUrlDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetContractDocumentQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDocumentUrlDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetContractDocumentUrl")
        .WithSummary("Lấy Presigned URL có thời hạn để xem/tải file PDF hợp đồng bảo mật")
        .Produces<ApiResponse<ContractDocumentUrlDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDocumentUrlDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDocumentUrlDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDocumentUrlDto>>(StatusCodes.Status404NotFound);

        // 5.1. Yêu cầu mã xác thực OTP để ký hợp đồng (Gửi qua email đã xác thực)
        group.MapPost("/{id:long}/request-otp", async (
            long id,
            [FromServices] RequestSignatureOtpHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<RequestSignatureOtpResult>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new RequestSignatureOtpCommand(id, userId);
            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<RequestSignatureOtpResult>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("RequestSignatureOtp")
        .WithSummary("Yêu cầu sinh mã OTP xác thực ký hợp đồng điện tử và gửi qua email")
        .Produces<ApiResponse<RequestSignatureOtpResult>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<RequestSignatureOtpResult>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<RequestSignatureOtpResult>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<RequestSignatureOtpResult>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<RequestSignatureOtpResult>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<RequestSignatureOtpResult>>(StatusCodes.Status409Conflict);

        // 6. Ký xác nhận hợp đồng điện tử bằng OTP (Người thuê hoặc Chủ nhà)
        group.MapPost("/{id:long}/sign", async (
            long id,
            [FromBody] SignContractRequest request,
            [FromServices] SignContractHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu ký hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new SignContractCommand(
                ContractId: id,
                ActorId: userId,
                DocumentHash: request.DocumentHash,
                OtpCode: request.OtpCode,
                SignatureMethod: request.SignatureMethod);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("SignContract")
        .WithSummary("Ký xác nhận hợp đồng điện tử (Người thuê hoặc Chủ nhà)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 7. Từ chối ký hợp đồng (Người thuê hoặc Chủ nhà)
        group.MapPost("/{id:long}/reject", async (
            long id,
            [FromBody] RejectContractRequest request,
            [FromServices] RejectContractHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu từ chối hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new RejectContractCommand(
                ContractId: id,
                ActorId: userId,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("RejectContract")
        .WithSummary("Từ chối ký hợp đồng (PENDING_SIGNATURE -> REJECTED)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 8. Hủy hợp đồng thuê khi chưa có hiệu lực - Dành cho Chủ nhà
        group.MapPost("/{id:long}/cancel", async (
            long id,
            [FromBody] CancelContractRequest request,
            [FromServices] CancelContractHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu hủy hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new CancelContractCommand(
                ContractId: id,
                LandlordId: userId,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("CancelContract")
        .WithSummary("Hủy hợp đồng thuê khi chưa có hiệu lực (Dành cho Chủ nhà)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 9. Kích hoạt hợp đồng thuê khi đã đến ngày bắt đầu - Dành cho Chủ nhà / Hệ thống
        group.MapPost("/{id:long}/activate", async (
            long id,
            [FromServices] ActivateContractHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var _))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new ActivateContractCommand(id);
            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("ActivateContract")
        .WithSummary("Kích hoạt hợp đồng thuê khi đã đủ điều kiện ngày bắt đầu")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 10. Xác nhận bàn giao phòng khi hết hạn thuê hoặc chấm dứt trước hạn - Dành cho Chủ nhà
        group.MapPost("/{id:long}/confirm-handover", async (
            long id,
            [FromBody] ConfirmHandoverRequest? request,
            [FromServices] ConfirmHandoverHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new ConfirmHandoverCommand(
                ContractId: id,
                LandlordId: userId,
                ActualOccupancyEndDate: request?.ActualOccupancyEndDate);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("ConfirmHandover")
        .WithSummary("Xác nhận bàn giao phòng khi trả phòng hoặc hết hạn thuê (Dành cho Chủ nhà)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 11. Chấm dứt hợp đồng thuê trước hạn - Dành cho Chủ nhà
        group.MapPost("/{id:long}/terminate", async (
            long id,
            [FromBody] TerminateContractRequest request,
            [FromServices] TerminateContractHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu chấm dứt hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new TerminateContractCommand(
                ContractId: id,
                LandlordId: userId,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("TerminateContract")
        .WithSummary("Chấm dứt hợp đồng thuê trước hạn (ACTIVE -> TERMINATED_PENDING_HANDOVER)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        // 12. Ghi nhận giao dịch tiền cọc (Thu cọc, Hoàn cọc, Khấu trừ) - Dành cho Chủ nhà
        group.MapPost("/{id:long}/deposits", async (
            long id,
            [FromBody] RecordDepositTransactionRequest request,
            [FromServices] RecordDepositTransactionHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DepositTransactionDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu giao dịch tiền cọc không hợp lệ.", validationErrors)));
            }

            var command = new RecordDepositTransactionCommand(
                ContractId: id,
                LandlordId: userId,
                TransactionType: request.TransactionType,
                Amount: request.Amount,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositTransactionDto>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(result.Value));
        })
        .WithName("RecordDepositTransaction")
        .WithSummary("Ghi nhận giao dịch tiền cọc: Thu cọc, Hoàn cọc hoặc Khấu trừ (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status409Conflict);

        // 13. Xem lịch sử giao dịch tiền cọc và số dư hiện tại của hợp đồng
        group.MapGet("/{id:long}/deposits", async (
            long id,
            [FromServices] GetDepositTransactionsHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DepositHistoryDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDepositTransactionsQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositHistoryDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetDepositTransactions")
        .WithSummary("Xem lịch sử các giao dịch tiền cọc và số dư hiện tại của hợp đồng")
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status404NotFound);

        // 14. Ghi nhận tiền cọc thực nhận (POST /contracts/{id}/deposits/received) - Dành cho Chủ nhà
        group.MapPost("/{id:long}/deposits/received", async (
            long id,
            [FromBody] RecordDepositReceivedRequest request,
            [FromServices] RecordDepositTransactionHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DepositTransactionDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu thu tiền cọc không hợp lệ.", validationErrors)));
            }

            var command = new RecordDepositTransactionCommand(
                ContractId: id,
                LandlordId: userId,
                TransactionType: DepositTransactionType.Received,
                Amount: request.Amount,
                Reason: null);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositTransactionDto>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(result.Value));
        })
        .WithName("RecordDepositReceived")
        .WithSummary("Ghi nhận khoản tiền cọc thực nhận (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status409Conflict);

        // 15. Ghi nhận hoàn trả tiền cọc (POST /contracts/{id}/deposits/refunds) - Dành cho Chủ nhà
        group.MapPost("/{id:long}/deposits/refunds", async (
            long id,
            [FromBody] RecordDepositRefundRequest request,
            [FromServices] RecordDepositTransactionHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DepositTransactionDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu hoàn trả tiền cọc không hợp lệ.", validationErrors)));
            }

            var command = new RecordDepositTransactionCommand(
                ContractId: id,
                LandlordId: userId,
                TransactionType: DepositTransactionType.Refunded,
                Amount: request.Amount,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositTransactionDto>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(result.Value));
        })
        .WithName("RecordDepositRefund")
        .WithSummary("Ghi nhận khoản tiền cọc hoàn trả cho người thuê (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status409Conflict);

        // 16. Ghi nhận khấu trừ tiền cọc (POST /contracts/{id}/deposits/deductions) - Dành cho Chủ nhà
        group.MapPost("/{id:long}/deposits/deductions", async (
            long id,
            [FromBody] RecordDepositDeductionRequest request,
            [FromServices] RecordDepositTransactionHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DepositTransactionDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionDto>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu khấu trừ tiền cọc không hợp lệ.", validationErrors)));
            }

            var command = new RecordDepositTransactionCommand(
                ContractId: id,
                LandlordId: userId,
                TransactionType: DepositTransactionType.Deducted,
                Amount: request.Amount,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositTransactionDto>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(result.Value));
        })
        .WithName("RecordDepositDeduction")
        .WithSummary("Ghi nhận khoản khấu trừ tiền cọc kèm lý do bồi thường/vệ sinh (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionDto>>(StatusCodes.Status409Conflict);

        // 17. Xem tóm tắt số dư tiền cọc hiện tại (GET /contracts/{id}/deposits/balance)
        group.MapGet("/{id:long}/deposits/balance", async (
            long id,
            [FromServices] GetDepositBalanceHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DepositBalanceDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDepositBalanceQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositBalanceDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetDepositBalance")
        .WithSummary("Xem tóm tắt số dư tiền cọc hiện tại của hợp đồng (Chủ nhà hoặc Người thuê)")
        .Produces<ApiResponse<DepositBalanceDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<DepositBalanceDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositBalanceDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositBalanceDto>>(StatusCodes.Status404NotFound);

        // 18. Xem danh sách toàn bộ các giao dịch tiền cọc (GET /contracts/{id}/deposits/transactions)
        group.MapGet("/{id:long}/deposits/transactions", async (
            long id,
            [FromServices] GetDepositTransactionsHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<DepositHistoryDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDepositTransactionsQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositHistoryDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetDepositTransactionList")
        .WithSummary("Xem danh sách toàn bộ các giao dịch tiền cọc của hợp đồng")
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositHistoryDto>>(StatusCodes.Status404NotFound);

        // 19. Xem trạng thái bàn giao phòng & chiếm dụng (GET /contracts/{id}/handover)
        group.MapGet("/{id:long}/handover", async (
            long id,
            [FromServices] GetHandoverDetailsHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<HandoverDetailsDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetHandoverDetailsQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<HandoverDetailsDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetHandoverDetails")
        .WithSummary("Xem trạng thái bàn giao phòng và tình trạng chiếm dụng phòng của hợp đồng")
        .Produces<ApiResponse<HandoverDetailsDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<HandoverDetailsDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<HandoverDetailsDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<HandoverDetailsDto>>(StatusCodes.Status404NotFound);

        // 20. Xác nhận bàn giao phòng (POST /contracts/{id}/handover/confirm) - Route alias
        group.MapPost("/{id:long}/handover/confirm", async (
            long id,
            [FromBody] ConfirmHandoverRequest? request,
            [FromServices] ConfirmHandoverHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractDto>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new ConfirmHandoverCommand(
                ContractId: id,
                LandlordId: userId,
                ActualOccupancyEndDate: request?.ActualOccupancyEndDate);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("ConfirmHandoverAlias")
        .WithSummary("Xác nhận bàn giao phòng khi kết thúc hợp đồng (Route alias)")
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractDto>>(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static bool TryGetUserId(HttpContext httpContext, out long userId)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out userId);
    }

    private static IResult MapFailureResult<T>(Error error)
    {
        var statusCode = error.Code switch
        {
            "Leasing.NotFound" or "Leasing.RoomNotFound" or "Leasing.TenantNotFound" or "Leasing.UserEmailNotFound" => StatusCodes.Status404NotFound,
            "Leasing.Forbidden" or "Leasing.RoomNotOwnedByLandlord" or "Leasing.OnlyLandlordCanConfirmHandover" or "Leasing.UnauthorizedSigner" => StatusCodes.Status403Forbidden,
            "Leasing.AuthenticationRequired" => StatusCodes.Status401Unauthorized,
            "Leasing.ScheduleConflict" or "Leasing.InvalidStatusTransition" or "Leasing.CannotModifyNonDraft" or "Leasing.CannotCancelActiveContract" or "Leasing.CannotTerminateNonActiveContract" or "Leasing.AlreadySignedByParty" or "Leasing.SignatureExpired" or "Leasing.HandoverNotPermitted" or "Leasing.HandoverAlreadyConfirmed" or "Leasing.DocumentHashMismatch" or "Leasing.DepositNotPermittedInStatus" => StatusCodes.Status409Conflict,
            "Leasing.InvalidOtp" or "Leasing.SelfLeasingNotAllowed" or "Leasing.InvalidDateRange" or "Leasing.InvalidMonthlyRent" or "Leasing.InvalidDepositAmount" or "Leasing.InvalidBillingDueDay" or "Leasing.SignatureEvidenceRequired" or "Leasing.InvalidHandoverDate" or "Leasing.InvalidDepositTransactionType" or "Leasing.InvalidDepositTransactionAmount" or "Leasing.DeductionReasonRequired" or "Leasing.DepositExceedsAgreed" or "Leasing.InsufficientDepositBalance" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(ApiResponse.Failure<T>(error), statusCode: statusCode);
    }
}
