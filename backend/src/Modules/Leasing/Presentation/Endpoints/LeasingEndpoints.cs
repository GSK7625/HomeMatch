using System.Security.Claims;
using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints;

public static class LeasingEndpoints
{
    public static IEndpointRouteBuilder MapLeasingApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1").WithTags("Leasing");

        #region UC3.7.1 — Khách quan tâm (Leads)

        group.MapGet("/landlord/leads", async (
            long? listingId,
            LeadStatus? status,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.GetLandlordLeadsAsync(landlordUserId, listingId, status, ct);
            return ToHttpResult(result);
        })
        .WithName("GetLandlordLeads")
        .WithSummary("Lấy danh sách khách quan tâm theo tin/phòng");

        group.MapGet("/landlord/leads/{id:long}", async (
            long id,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.GetLeadByIdAsync(landlordUserId, id, ct);
            return ToHttpResult(result);
        })
        .WithName("GetLeadById")
        .WithSummary("Xem chi tiết khách quan tâm");

        group.MapPatch("/landlord/leads/{id:long}/status", async (
            long id,
            UpdateLeadRequest request,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.UpdateLeadStatusAsync(landlordUserId, id, request, ct);
            return ToHttpResult(result);
        })
        .WithName("UpdateLeadStatus")
        .WithSummary("Cập nhật trạng thái xử lý và ghi chú khách quan tâm");

        #endregion

        #region UC3.7.2 — Quản lý danh sách người thuê (Tenants)

        group.MapGet("/landlord/tenants", async (
            long? roomId,
            TenantLifecycleStatus? status,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.GetLandlordTenantsAsync(landlordUserId, roomId, status, ct);
            return ToHttpResult(result);
        })
        .WithName("GetLandlordTenants")
        .WithSummary("Lấy danh sách người thuê theo phòng và trạng thái thuê");

        group.MapGet("/landlord/tenants/{contractId:long}", async (
            long contractId,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.GetTenantDetailAsync(landlordUserId, contractId, ct);
            return ToHttpResult(result);
        })
        .WithName("GetTenantDetail")
        .WithSummary("Xem thông tin chi tiết người thuê và hợp đồng liên quan");

        #endregion

        #region UC3.7.3 — Soạn và quản lý hợp đồng thuê (Contracts)

        group.MapPost("/contracts", async (
            CreateContractDraftRequest request,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.CreateContractDraftAsync(landlordUserId, request, ct);
            return result.IsSuccess 
                ? Results.Created($"/api/v1/contracts/{result.Value.Id}", ApiResponse<ContractDto>.Success(result.Value))
                : ToHttpResult(result);
        })
        .WithName("CreateContractDraft")
        .WithSummary("Soạn bản nháp hợp đồng mới");

        group.MapPut("/contracts/{id:long}", async (
            long id,
            UpdateContractDraftRequest request,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.UpdateContractDraftAsync(landlordUserId, id, request, ct);
            return ToHttpResult(result);
        })
        .WithName("UpdateContractDraft")
        .WithSummary("Chỉnh sửa điều khoản hợp đồng bản nháp");

        group.MapPost("/contracts/{id:long}/send", async (
            long id,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.SendContractToTenantAsync(landlordUserId, id, ct);
            return ToHttpResult(result);
        })
        .WithName("SendContractToTenant")
        .WithSummary("Gửi hợp đồng cho người thuê xem xét và xác nhận");

        group.MapPost("/contracts/{id:long}/tenant-confirm", async (
            long id,
            TenantConfirmContractRequest request,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var tenantUserId = GetUserId(user);
            var result = await leasingService.TenantConfirmOrRejectContractAsync(tenantUserId, id, request, ct);
            return ToHttpResult(result);
        })
        .WithName("TenantConfirmOrRejectContract")
        .WithSummary("Người thuê xác nhận đồng ý hoặc từ chối hợp đồng");

        group.MapPost("/contracts/{id:long}/landlord-finalize", async (
            long id,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.LandlordFinalizeContractAsync(landlordUserId, id, ct);
            return ToHttpResult(result);
        })
        .WithName("LandlordFinalizeContract")
        .WithSummary("Chủ nhà xác nhận lần cuối và kích hoạt hợp đồng (ACTIVE)");

        group.MapGet("/contracts/{id:long}", async (
            long id,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var currentUserId = GetUserId(user);
            var result = await leasingService.GetContractByIdAsync(currentUserId, id, ct);
            return ToHttpResult(result);
        })
        .WithName("GetContractById")
        .WithSummary("Xem chi tiết hợp đồng thuê");

        group.MapPost("/contracts/{id:long}/cancel", async (
            long id,
            CancelContractRequest request,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var currentUserId = GetUserId(user);
            var result = await leasingService.CancelContractAsync(currentUserId, id, request, ct);
            return ToHttpResult(result);
        })
        .WithName("CancelContract")
        .WithSummary("Hủy hợp đồng khi chưa kích hoạt");

        group.MapPost("/contracts/{id:long}/terminate", async (
            long id,
            TerminateContractRequest request,
            ClaimsPrincipal user,
            ILeasingService leasingService,
            CancellationToken ct) =>
        {
            var landlordUserId = GetUserId(user);
            var result = await leasingService.TerminateContractAsync(landlordUserId, id, request, ct);
            return ToHttpResult(result);
        })
        .WithName("TerminateContract")
        .WithSummary("Thanh lý / chấm dứt hợp đồng đang có hiệu lực");

        #endregion

        return endpoints;
    }

    private static IResult ToHttpResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(ApiResponse<T>.Success(result.Value));
        }

        var error = result.Error;
        if (error.Code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Json(ApiResponse<T>.Failure(error), statusCode: StatusCodes.Status403Forbidden);
        }

        if (error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Json(ApiResponse<T>.Failure(error), statusCode: StatusCodes.Status404NotFound);
        }

        return Results.BadRequest(ApiResponse<T>.Failure(error));
    }

    private static long GetUserId(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value
                   ?? user.FindFirst("uid")?.Value;

        return long.TryParse(idClaim, out var id) ? id : 1; // Default fallback for development/testing
    }
}
