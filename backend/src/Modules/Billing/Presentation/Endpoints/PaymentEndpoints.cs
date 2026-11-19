using System.Security.Claims;
using HomeMatch.Modules.Billing.Application.Commands.CreatePayment;
using HomeMatch.Modules.Billing.Application.Commands.PayContractDeposit;
using HomeMatch.Modules.Billing.Application.Commands.ProcessPaymentCallback;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Queries.GetPaymentById;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Modules.Billing.Infrastructure.Services;
using HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Billing.Presentation.Endpoints;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this RouteGroupBuilder group)
    {
        // 1. Tạo giao dịch thanh toán cho hóa đơn (Tenant/Admin)
        group.MapPost("/invoices/{invoiceId:long}/payments", async (
            [FromRoute] long invoiceId,
            [FromServices] CreatePaymentHandler handler,
            HttpContext httpContext,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PaymentResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var clientIp = GetClientIpAddress(httpContext);
            var command = new CreatePaymentCommand(
                invoiceId,
                userId,
                IsAdmin(user),
                clientIp);

            var result = await handler.HandleAsync(command, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<PaymentResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Created($"/api/v1/billing/payments/{response.Id}", ApiResponse.Success(response));
        })
        .RequireAuthorization()
        .WithName("CreatePayment")
        .WithSummary("Người thuê khởi tạo giao dịch thanh toán hóa đơn qua cổng VNPay");

        // 2. Tra cứu chi tiết một giao dịch thanh toán (Tenant/Landlord/Admin)
        group.MapGet("/payments/{id:long}", async (
            [FromRoute] long id,
            [FromServices] GetPaymentByIdHandler handler,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PaymentResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var query = new GetPaymentByIdQuery(id, userId, IsAdmin(user));
            var result = await handler.HandleAsync(query, cancellationToken);
            if (result.IsFailure)
            {
                return ToErrorResult<PaymentResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Ok(ApiResponse.Success(response));
        })
        .RequireAuthorization()
        .WithName("GetPaymentById")
        .WithSummary("Xem thông tin chi tiết một giao dịch thanh toán");

        // 3. User Return URL (Trình duyệt người dùng được redirect về sau khi thanh toán trên VNPay)
        group.MapGet("/payments/vnpay-return", async (
            [FromServices] ProcessPaymentCallbackHandler handler,
            [FromServices] IOptions<VNPayOptions> vnpayOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var queryParams = httpContext.Request.Query
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
                .ToList();

            var result = await handler.HandleAsync(new ProcessPaymentCallbackCommand(queryParams), cancellationToken);

            var frontendBaseUrl = vnpayOptions.Value.ReturnUrl;
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                frontendBaseUrl = "http://localhost:3000/billing/payment-callback";
            }

            var redirectUrl = $"{frontendBaseUrl}" +
                              $"?paymentCode={result.Value.Payment?.PaymentCode}" +
                              $"&invoiceId={result.Value.Payment?.InvoiceId}" +
                              $"&status={result.Value.Payment?.Status}" +
                              $"&isSuccess={result.Value.IsSuccess}" +
                              $"&message={Uri.EscapeDataString(result.Value.ErrorMessage ?? string.Empty)}";

            return Results.Redirect(redirectUrl);
        })
        .AllowAnonymous()
        .WithName("VNPayReturn")
        .WithSummary("Điểm tiếp nhận khi người dùng hoàn tất thanh toán từ VNPay quay lại hệ thống");

        // 4. IPN Webhook (Server-to-Server từ VNPay, xử lý idempotent)
        group.MapGet("/payments/vnpay-ipn", async (
            [FromServices] ProcessPaymentCallbackHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var queryParams = httpContext.Request.Query
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
                .ToList();

            var result = await handler.HandleAsync(new ProcessPaymentCallbackCommand(queryParams), cancellationToken);

            // VNPay yêu cầu trả về JSON với cấu trúc {"RspCode":"...","Message":"..."}
            return Results.Json(result.Value.IpnResponse);
        })
        .AllowAnonymous()
        .WithName("VNPayIpn")
        .WithSummary("Webhook IPN nhận kết quả thanh toán bất đồng bộ từ cổng VNPay");

        // 5. Người thuê khởi tạo thanh toán tiền cọc / kích hoạt hợp đồng sau khi ký (SIGNED -> ACTIVE)
        group.MapPost("/contracts/{contractId:long}/pay-deposit", async (
            [FromRoute] long contractId,
            [FromServices] PayContractDepositHandler handler,
            HttpContext httpContext,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PaymentResponse>(BillingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var clientIp = GetClientIpAddress(httpContext);
            var result = await handler.HandleAsync(
                new PayContractDepositCommand(contractId, userId, IsAdmin(user), clientIp),
                cancellationToken);

            if (result.IsFailure)
            {
                return ToErrorResult<PaymentResponse>(result.Error);
            }

            var response = InvoiceResponseMapper.ToResponse(result.Value);
            return Results.Ok(ApiResponse.Success(response));
        })
        .RequireAuthorization()
        .WithName("PayContractDeposit")
        .WithSummary("Khởi tạo giao dịch VNPay thanh toán tiền cọc và kích hoạt hợp đồng sau khi ký");

        // 6. Xác thực kết quả trả về từ VNPay / Sandbox ngay trên Frontend callback page (JSON)
        group.MapGet("/payments/verify-return", async (
            [FromServices] ProcessPaymentCallbackHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var queryParams = httpContext.Request.Query
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
                .ToList();

            var result = await handler.HandleAsync(new ProcessPaymentCallbackCommand(queryParams), cancellationToken);
            var val = result.Value;

            return Results.Ok(ApiResponse.Success(new
            {
                isSuccess = val.IsSuccess,
                errorMessage = val.ErrorMessage,
                contractId = val.ContractId,
                contractActivated = val.ContractActivated,
                payment = val.Payment is null ? null : InvoiceResponseMapper.ToResponse(val.Payment)
            }));
        })
        .AllowAnonymous()
        .WithName("VerifyVNPayReturn")
        .WithSummary("Xác thực chữ ký VNPay Return URL và trả JSON cho Frontend");

        // 7. Mô phỏng ký xác thực giao dịch Sandbox khi chạy môi trường dev (không có VNPAY_TMN_CODE thật)
        group.MapPost("/payments/sandbox-confirm", async (
            [FromBody] SandboxConfirmRequest request,
            [FromServices] IPaymentRepository paymentRepository,
            [FromServices] IVNPayGateway vnpayGateway,
            CancellationToken cancellationToken) =>
        {
            if (!vnpayGateway.IsSandboxSimulatorMode)
            {
                return Results.Json(
                    ApiResponse.Failure<object>(new Error("Billing.SandboxDisabled", "Cổng giả lập chỉ hoạt động khi chưa cấu hình VNPAY_TMN_CODE thật.")),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var payment = await paymentRepository.GetByCodeAsync(request.PaymentCode, cancellationToken);
            if (payment is null)
            {
                return Results.Json(
                    ApiResponse.Failure<object>(BillingErrors.PaymentNotFound),
                    statusCode: StatusCodes.Status404NotFound);
            }

            var callbackQuery = vnpayGateway.CreateSandboxCallbackQueryString(
                payment.PaymentCode,
                payment.Amount,
                $"Thanh toan hoa don #{payment.InvoiceId}",
                request.IsSuccess,
                string.IsNullOrWhiteSpace(request.BankCode) ? "NCB" : request.BankCode);

            return Results.Ok(ApiResponse.Success(new
            {
                callbackUrl = $"/billing/payment-callback?{callbackQuery}"
            }));
        })
        .AllowAnonymous()
        .WithName("SandboxConfirmPayment")
        .WithSummary("Tạo chữ ký VNPay hợp lệ cho giao dịch giả lập ở môi trường Dev");

        return group;
    }

    private sealed record SandboxConfirmRequest(string PaymentCode, bool IsSuccess, string? BankCode = "NCB");

    private static string GetClientIpAddress(HttpContext httpContext)
    {
        var ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        if (string.IsNullOrWhiteSpace(ip) || ip == "::1")
        {
            ip = "127.0.0.1";
        }

        return ip;
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

    private static IResult ToErrorResult<T>(Error error)
    {
        int statusCode = error.Code switch
        {
            "Billing.AuthenticationRequired" => StatusCodes.Status401Unauthorized,
            "Billing.Forbidden" => StatusCodes.Status403Forbidden,
            "Billing.PaymentNotFound" => StatusCodes.Status404NotFound,
            "Billing.InvoiceNotFound" => StatusCodes.Status404NotFound,
            "Billing.PendingPaymentAlreadyExists" => StatusCodes.Status409Conflict,
            "Billing.InvoiceAlreadyPaid" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(ApiResponse.Failure<T>(error), statusCode: statusCode);
    }
}
