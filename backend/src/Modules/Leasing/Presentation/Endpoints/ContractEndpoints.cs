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
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractById;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractDetails;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractDocumentUrl;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractsByLandlord;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractsByTenant;
using HomeMatch.Modules.Leasing.Application.Queries.GetDepositBalance;
using HomeMatch.Modules.Leasing.Application.Queries.GetDepositTransactions;
using HomeMatch.Modules.Leasing.Application.Queries.GetExpiringContracts;
using HomeMatch.Modules.Leasing.Application.Queries.GetHandoverDetails;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;
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

        // 1. Tạo hợp đồng thuê mới (Chủ nhà nhập form + gửi kèm file PDF xuất từ bản preview)
        group.MapPost("", async (
            [AsParameters] CreateContractFormRequest request,
            IFormFile? file,
            [FromServices] CreateContractHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractResponse>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu tạo hợp đồng thuê không hợp lệ.", validationErrors)));
            }

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

            var command = new CreateContractCommand(
                LandlordId: userId,
                RoomId: request.RoomId,
                TenantId: request.TenantId,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                MonthlyRent: request.MonthlyRent,
                DepositAmount: request.DepositAmount,
                BillingDueDay: request.BillingDueDay,
                ContractCode: request.ContractCode,
                FileStream: fileStream,
                FileName: fileName,
                ContentType: contentType,
                FileLength: fileLength,
                DocumentSnapshot: request.DocumentSnapshot,
                SignatureExpiresAt: request.SignatureExpiresAt,
                HandoverDueAt: request.HandoverDueAt);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{result.Value.Id}",
                ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .DisableAntiforgery()
        .WithName("CreateContract")
        .WithSummary("Tạo hợp đồng thuê mới (Chủ nhà nhập form + gửi kèm file PDF xuất từ bản preview)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<PagedContractsResponse>(LeasingErrors.AuthenticationRequired),
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
                    return MapFailureResult<PagedContractsResponse>(result.Error);
                }

                return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
            }
            else
            {
                var query = new GetContractsByLandlordQuery(userId, status, page, pageSize);
                var result = await landlordHandler.HandleAsync(query, cancellationToken);
                if (result.IsFailure)
                {
                    return MapFailureResult<PagedContractsResponse>(result.Error);
                }

                return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
            }
        })
        .WithName("GetContracts")
        .WithSummary("Xem danh sách hợp đồng thuê có phân trang (Chủ nhà hoặc Người thuê)")
        .Produces<ApiResponse<PagedContractsResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PagedContractsResponse>>(StatusCodes.Status401Unauthorized);

        // 2.1. Lấy danh sách hợp đồng sắp hết hạn trong N ngày tới - Dành cho Chủ nhà (BR-Phase7)
        group.MapGet("/expiring", async (
            [FromQuery] int days = 30,
            [FromServices] GetExpiringContractsHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<IReadOnlyList<ContractResponse>>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetExpiringContractsQuery(userId, days);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<IReadOnlyList<ContractResponse>>(result.Error);
            }

            var responseList = result.Value.Select(ContractResponseMapper.ToResponse).ToList();
            return Results.Ok(ApiResponse.Success<IReadOnlyList<ContractResponse>>(responseList));
        })
        .WithName("GetExpiringContracts")
        .WithSummary("Xem danh sách hợp đồng sắp hết hạn trong vòng N ngày tới (Chủ nhà)")
        .Produces<ApiResponse<IReadOnlyList<ContractResponse>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<IReadOnlyList<ContractResponse>>>(StatusCodes.Status401Unauthorized);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(new GetContractByIdQuery(id, userId), cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("GetContractById")
        .WithSummary("Xem thông tin hợp đồng thuê theo ID")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound);

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
                    ApiResponse.Failure<ContractDetailResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(new GetContractDetailsQuery(id, userId), cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDetailResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("GetContractDetails")
        .WithSummary("Xem chi tiết hợp đồng thuê bao gồm tóm tắt tiền cọc")
        .Produces<ApiResponse<ContractDetailResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDetailResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDetailResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDetailResponse>>(StatusCodes.Status404NotFound);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractResponse>(
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
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("SendContractForSignature")
        .WithSummary("Gửi hợp đồng cho các bên ký trực tuyến (DRAFT -> PENDING_SIGNATURE)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<ContractDocumentUrlResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetContractDocumentQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractDocumentUrlResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("GetContractDocumentUrl")
        .WithSummary("Lấy Presigned URL có thời hạn để xem/tải file PDF hợp đồng bảo mật")
        .Produces<ApiResponse<ContractDocumentUrlResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractDocumentUrlResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractDocumentUrlResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractDocumentUrlResponse>>(StatusCodes.Status404NotFound);

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
                    ApiResponse.Failure<RequestSignatureOtpResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new RequestSignatureOtpCommand(id, userId);
            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<RequestSignatureOtpResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("RequestSignatureOtp")
        .WithSummary("Yêu cầu sinh mã OTP xác thực ký hợp đồng điện tử và gửi qua email")
        .Produces<ApiResponse<RequestSignatureOtpResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<RequestSignatureOtpResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<RequestSignatureOtpResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<RequestSignatureOtpResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<RequestSignatureOtpResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<RequestSignatureOtpResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractResponse>(
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
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("SignContract")
        .WithSummary("Ký xác nhận hợp đồng điện tử (Người thuê hoặc Chủ nhà)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractResponse>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu từ chối hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new RejectContractCommand(
                ContractId: id,
                ActorId: userId,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("RejectContract")
        .WithSummary("Từ chối ký hợp đồng (PENDING_SIGNATURE -> REJECTED)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractResponse>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu hủy hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new CancelContractCommand(
                ContractId: id,
                LandlordId: userId,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("CancelContract")
        .WithSummary("Hủy hợp đồng thuê khi chưa có hiệu lực (Dành cho Chủ nhà)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new ActivateContractCommand(id);
            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("ActivateContract")
        .WithSummary("Kích hoạt hợp đồng thuê khi đã đủ điều kiện ngày bắt đầu")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new ConfirmHandoverCommand(
                ContractId: id,
                LandlordId: userId,
                ActualOccupancyEndDate: request?.ActualOccupancyEndDate);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("ConfirmHandover")
        .WithSummary("Xác nhận bàn giao phòng khi trả phòng hoặc hết hạn thuê (Dành cho Chủ nhà)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ContractResponse>(
                    Error.Validation("Leasing.ValidationFailed", "Dữ liệu chấm dứt hợp đồng không hợp lệ.", validationErrors)));
            }

            var command = new TerminateContractCommand(
                ContractId: id,
                LandlordId: userId,
                Reason: request.Reason);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("TerminateContract")
        .WithSummary("Chấm dứt hợp đồng thuê trước hạn (ACTIVE -> TERMINATED_PENDING_HANDOVER)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<DepositTransactionResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionResponse>(
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
                return MapFailureResult<DepositTransactionResponse>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("RecordDepositTransaction")
        .WithSummary("Ghi nhận giao dịch tiền cọc: Thu cọc, Hoàn cọc hoặc Khấu trừ (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<DepositHistoryResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDepositTransactionsQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositHistoryResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("GetDepositTransactions")
        .WithSummary("Xem lịch sử các giao dịch tiền cọc và số dư hiện tại của hợp đồng")
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status404NotFound);

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
                    ApiResponse.Failure<DepositTransactionResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionResponse>(
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
                return MapFailureResult<DepositTransactionResponse>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("RecordDepositReceived")
        .WithSummary("Ghi nhận khoản tiền cọc thực nhận (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<DepositTransactionResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionResponse>(
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
                return MapFailureResult<DepositTransactionResponse>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("RecordDepositRefund")
        .WithSummary("Ghi nhận khoản tiền cọc hoàn trả cho người thuê (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<DepositTransactionResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<DepositTransactionResponse>(
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
                return MapFailureResult<DepositTransactionResponse>(result.Error);
            }

            return Results.Created(
                $"/api/v1/leasing/contracts/{id}/deposits/{result.Value.Id}",
                ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("RecordDepositDeduction")
        .WithSummary("Ghi nhận khoản khấu trừ tiền cọc kèm lý do bồi thường/vệ sinh (Dành cho Chủ nhà)")
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<DepositTransactionResponse>>(StatusCodes.Status409Conflict);

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
                    ApiResponse.Failure<DepositBalanceResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDepositBalanceQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositBalanceResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("GetDepositBalance")
        .WithSummary("Xem tóm tắt số dư tiền cọc hiện tại của hợp đồng (Chủ nhà hoặc Người thuê)")
        .Produces<ApiResponse<DepositBalanceResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<DepositBalanceResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositBalanceResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositBalanceResponse>>(StatusCodes.Status404NotFound);

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
                    ApiResponse.Failure<DepositHistoryResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetDepositTransactionsQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<DepositHistoryResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("GetDepositTransactionList")
        .WithSummary("Xem danh sách toàn bộ các giao dịch tiền cọc của hợp đồng")
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<DepositHistoryResponse>>(StatusCodes.Status404NotFound);

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
                    ApiResponse.Failure<HandoverDetailsResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetHandoverDetailsQuery(id, userId);
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<HandoverDetailsResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("GetHandoverDetails")
        .WithSummary("Xem trạng thái bàn giao phòng và tình trạng chiếm dụng phòng của hợp đồng")
        .Produces<ApiResponse<HandoverDetailsResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<HandoverDetailsResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<HandoverDetailsResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<HandoverDetailsResponse>>(StatusCodes.Status404NotFound);

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
                    ApiResponse.Failure<ContractResponse>(LeasingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new ConfirmHandoverCommand(
                ContractId: id,
                LandlordId: userId,
                ActualOccupancyEndDate: request?.ActualOccupancyEndDate);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return MapFailureResult<ContractResponse>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(ContractResponseMapper.ToResponse(result.Value)));
        })
        .WithName("ConfirmHandoverAlias")
        .WithSummary("Xác nhận bàn giao phòng khi kết thúc hợp đồng (Route alias)")
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<ContractResponse>>(StatusCodes.Status409Conflict);

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
