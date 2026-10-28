using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Entities;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Modules.Leasing.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Leasing.Infrastructure.Services;

public class LeasingService : ILeasingService
{
    private readonly LeasingDbContext _context;
    private readonly IRoomOwnershipService _roomOwnershipService;

    public LeasingService(LeasingDbContext context, IRoomOwnershipService roomOwnershipService)
    {
        _context = context;
        _roomOwnershipService = roomOwnershipService;
    }

    #region UC3.7.1 — Quản lý khách quan tâm (Leads)

    public async Task<Result<IEnumerable<LeadDto>>> GetLandlordLeadsAsync(
        long landlordUserId, 
        long? listingId = null, 
        LeadStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();

        if (listingId.HasValue)
        {
            query = query.Where(l => l.ListingId == listingId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        var leads = await query
            .OrderByDescending(l => l.UpdatedAt)
            .Select(l => new LeadDto(
                l.Id,
                l.TenantId,
                l.ListingId,
                l.RoomId,
                l.Status,
                l.Notes,
                l.CreatedAt,
                l.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<LeadDto>>(leads);
    }

    public async Task<Result<LeadDto>> GetLeadByIdAsync(
        long landlordUserId, 
        long leadId, 
        CancellationToken cancellationToken = default)
    {
        var lead = await _context.Leads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);

        if (lead is null)
        {
            return Result.Failure<LeadDto>(new Error("Lead.NotFound", "Không tìm thấy thông tin khách quan tâm."));
        }

        return Result.Success(new LeadDto(
            lead.Id,
            lead.TenantId,
            lead.ListingId,
            lead.RoomId,
            lead.Status,
            lead.Notes,
            lead.CreatedAt,
            lead.UpdatedAt));
    }

    public async Task<Result<LeadDto>> UpdateLeadStatusAsync(
        long landlordUserId, 
        long leadId, 
        UpdateLeadRequest request, 
        CancellationToken cancellationToken = default)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
        if (lead is null)
        {
            return Result.Failure<LeadDto>(new Error("Lead.NotFound", "Không tìm thấy thông tin khách quan tâm."));
        }

        try
        {
            lead.UpdateStatus(request.Status, request.Note);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(new LeadDto(
                lead.Id,
                lead.TenantId,
                lead.ListingId,
                lead.RoomId,
                lead.Status,
                lead.Notes,
                lead.CreatedAt,
                lead.UpdatedAt));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<LeadDto>(new Error("Lead.InvalidTransition", ex.Message));
        }
    }

    #endregion

    #region UC3.7.2 — Quản lý danh sách người thuê (Tenants)

    public async Task<Result<IEnumerable<TenantSummaryDto>>> GetLandlordTenantsAsync(
        long landlordUserId, 
        long? roomId = null, 
        TenantLifecycleStatus? statusFilter = null, 
        CancellationToken cancellationToken = default)
    {
        // Danh sách người thuê bắt buộc phải dựa trên các quan hệ hợp đồng thực tế (ACTIVE, COMPLETED, TERMINATED)
        var query = _context.Contracts.AsNoTracking()
            .Where(c => c.LandlordId == landlordUserId && 
                       (c.Status == ContractStatus.ACTIVE || 
                        c.Status == ContractStatus.COMPLETED || 
                        c.Status == ContractStatus.TERMINATED));

        if (roomId.HasValue)
        {
            query = query.Where(c => c.RoomId == roomId.Value);
        }

        var contracts = await query
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var list = contracts.Select(c =>
        {
            TenantLifecycleStatus lifecycle;
            if (c.Status == ContractStatus.ACTIVE)
            {
                var daysRemaining = c.EndDate.DayNumber - today.DayNumber;
                lifecycle = (daysRemaining is >= 0 and <= 30) 
                    ? TenantLifecycleStatus.EXPIRING_SOON 
                    : TenantLifecycleStatus.RENTING;
            }
            else
            {
                lifecycle = TenantLifecycleStatus.MOVED_OUT;
            }

            return new TenantSummaryDto(
                c.TenantId,
                c.Id,
                c.ContractCode,
                c.RoomId,
                c.LandlordId,
                c.StartDate,
                c.EndDate,
                c.MonthlyRent,
                c.DepositAmount,
                c.DepositStatus,
                c.Status,
                lifecycle
            );
        });

        if (statusFilter.HasValue)
        {
            list = list.Where(t => t.LifecycleStatus == statusFilter.Value);
        }

        return Result.Success<IEnumerable<TenantSummaryDto>>(list.ToList());
    }

    public async Task<Result<TenantSummaryDto>> GetTenantDetailAsync(
        long landlordUserId, 
        long contractId, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId && c.LandlordId == landlordUserId, cancellationToken);

        if (contract is null)
        {
            return Result.Failure<TenantSummaryDto>(new Error("Tenant.NotFound", "Không tìm thấy người thuê hoặc hợp đồng liên quan."));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        TenantLifecycleStatus lifecycle;
        if (contract.Status == ContractStatus.ACTIVE)
        {
            var daysRemaining = contract.EndDate.DayNumber - today.DayNumber;
            lifecycle = (daysRemaining is >= 0 and <= 30) 
                ? TenantLifecycleStatus.EXPIRING_SOON 
                : TenantLifecycleStatus.RENTING;
        }
        else
        {
            lifecycle = TenantLifecycleStatus.MOVED_OUT;
        }

        var dto = new TenantSummaryDto(
            contract.TenantId,
            contract.Id,
            contract.ContractCode,
            contract.RoomId,
            contract.LandlordId,
            contract.StartDate,
            contract.EndDate,
            contract.MonthlyRent,
            contract.DepositAmount,
            contract.DepositStatus,
            contract.Status,
            lifecycle
        );

        return Result.Success(dto);
    }

    #endregion

    #region UC3.7.3 — Soạn và quản lý hợp đồng thuê (Contracts)

    public async Task<Result<ContractDto>> CreateContractDraftAsync(
        long landlordUserId, 
        CreateContractDraftRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra quyền sở hữu phòng của chủ nhà
        var isOwner = await _roomOwnershipService.IsLandlordOwnerOfRoomAsync(landlordUserId, request.RoomId, cancellationToken);
        if (!isOwner)
        {
            return Result.Failure<ContractDto>(new Error("Contract.Forbidden", "Chủ nhà không có quyền tạo hợp đồng cho phòng này."));
        }

        // 2. Kiểm tra xung đột phòng: phòng đã có hợp đồng ACTIVE trong khoảng thời gian này chưa
        var hasActiveConflict = await _context.Contracts.AnyAsync(c =>
            c.RoomId == request.RoomId &&
            c.Status == ContractStatus.ACTIVE &&
            c.StartDate < request.EndDate &&
            c.EndDate > request.StartDate,
            cancellationToken);

        if (hasActiveConflict)
        {
            return Result.Failure<ContractDto>(new Error("Contract.RoomConflict", "Phòng đã có hợp đồng thuê có hiệu lực trong khoảng thời gian này."));
        }

        // 3. Khởi tạo hợp đồng bản nháp
        var contractCode = $"HM-CTR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var contract = new Contract
        {
            ContractCode = contractCode,
            RoomId = request.RoomId,
            TenantId = request.TenantId,
            LandlordId = landlordUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MonthlyRent = request.MonthlyRent,
            DepositAmount = request.DepositAmount,
            BillingDueDay = request.BillingDueDay,
            TermsSnapshot = request.TermsSnapshot,
            Status = ContractStatus.DRAFT,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            // Kiểm tra tính hợp lệ cơ bản của ngày tháng
            contract.UpdateDraftTerms(request.StartDate, request.EndDate, request.MonthlyRent, request.DepositAmount, request.BillingDueDay, request.TermsSnapshot);
        }
        catch (Exception ex)
        {
            return Result.Failure<ContractDto>(new Error("Contract.InvalidTerms", ex.Message));
        }

        _context.Contracts.Add(contract);

        // Ghi nhận Audit Log
        _context.ContractAuditLogs.Add(new ContractAuditLog
        {
            ContractId = contract.Id,
            Action = "CREATED_DRAFT",
            PerformedByUserId = landlordUserId,
            PerformedByRole = "LANDLORD",
            Timestamp = DateTimeOffset.UtcNow,
            Notes = "Tạo bản nháp hợp đồng mới.",
            TermsSnapshot = request.TermsSnapshot
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(contract));
    }

    public async Task<Result<ContractDto>> UpdateContractDraftAsync(
        long landlordUserId, 
        long contractId, 
        UpdateContractDraftRequest request, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(new Error("Contract.NotFound", "Không tìm thấy hợp đồng."));
        }

        if (contract.LandlordId != landlordUserId)
        {
            return Result.Failure<ContractDto>(new Error("Contract.Forbidden", "Chủ nhà không có quyền chỉnh sửa hợp đồng này."));
        }

        try
        {
            contract.UpdateDraftTerms(
                request.StartDate, 
                request.EndDate, 
                request.MonthlyRent, 
                request.DepositAmount, 
                request.BillingDueDay, 
                request.TermsSnapshot);

            _context.ContractAuditLogs.Add(new ContractAuditLog
            {
                ContractId = contract.Id,
                Action = "UPDATED_DRAFT",
                PerformedByUserId = landlordUserId,
                PerformedByRole = "LANDLORD",
                Timestamp = DateTimeOffset.UtcNow,
                Notes = "Cập nhật điều khoản bản nháp hợp đồng.",
                TermsSnapshot = request.TermsSnapshot
            });

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(contract));
        }
        catch (Exception ex)
        {
            return Result.Failure<ContractDto>(new Error("Contract.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ContractDto>> SendContractToTenantAsync(
        long landlordUserId, 
        long contractId, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(new Error("Contract.NotFound", "Không tìm thấy hợp đồng."));
        }

        if (contract.LandlordId != landlordUserId)
        {
            return Result.Failure<ContractDto>(new Error("Contract.Forbidden", "Chủ nhà không có quyền thao tác trên hợp đồng này."));
        }

        try
        {
            var snapshot = contract.TermsSnapshot ?? $"Rent:{contract.MonthlyRent};Deposit:{contract.DepositAmount};From:{contract.StartDate:yyyy-MM-dd};To:{contract.EndDate:yyyy-MM-dd}";
            contract.SendToTenant(snapshot);

            _context.ContractAuditLogs.Add(new ContractAuditLog
            {
                ContractId = contract.Id,
                Action = "SENT_TO_TENANT",
                PerformedByUserId = landlordUserId,
                PerformedByRole = "LANDLORD",
                Timestamp = DateTimeOffset.UtcNow,
                Notes = "Chủ nhà gửi hợp đồng để người thuê xác nhận.",
                TermsSnapshot = snapshot
            });

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(contract));
        }
        catch (Exception ex)
        {
            return Result.Failure<ContractDto>(new Error("Contract.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ContractDto>> TenantConfirmOrRejectContractAsync(
        long tenantUserId, 
        long contractId, 
        TenantConfirmContractRequest request, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(new Error("Contract.NotFound", "Không tìm thấy hợp đồng."));
        }

        if (contract.TenantId != tenantUserId)
        {
            return Result.Failure<ContractDto>(new Error("Contract.Forbidden", "Người dùng không phải là người thuê của hợp đồng này."));
        }

        try
        {
            if (request.Accept)
            {
                contract.TenantConfirm(tenantUserId, request.TermsSnapshotToVerify);

                _context.ContractAuditLogs.Add(new ContractAuditLog
                {
                    ContractId = contract.Id,
                    Action = "TENANT_CONFIRMED",
                    PerformedByUserId = tenantUserId,
                    PerformedByRole = "TENANT",
                    Timestamp = DateTimeOffset.UtcNow,
                    Notes = "Người thuê đã đồng ý các điều khoản hợp đồng.",
                    TermsSnapshot = request.TermsSnapshotToVerify
                });
            }
            else
            {
                contract.TenantReject(tenantUserId, request.RejectionReason ?? "Từ chối xác nhận điều khoản hợp đồng.");

                _context.ContractAuditLogs.Add(new ContractAuditLog
                {
                    ContractId = contract.Id,
                    Action = "TENANT_REJECTED",
                    PerformedByUserId = tenantUserId,
                    PerformedByRole = "TENANT",
                    Timestamp = DateTimeOffset.UtcNow,
                    Notes = $"Người thuê từ chối: {request.RejectionReason}"
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(contract));
        }
        catch (Exception ex)
        {
            return Result.Failure<ContractDto>(new Error("Contract.ConfirmationError", ex.Message));
        }
    }

    public async Task<Result<ContractDto>> LandlordFinalizeContractAsync(
        long landlordUserId, 
        long contractId, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(new Error("Contract.NotFound", "Không tìm thấy hợp đồng."));
        }

        if (contract.LandlordId != landlordUserId)
        {
            return Result.Failure<ContractDto>(new Error("Contract.Forbidden", "Chủ nhà không có quyền kích hoạt hợp đồng này."));
        }

        // Chống Race Condition: Kiểm tra lại phòng có đang có hợp đồng ACTIVE trùng thời gian không
        var hasActiveConflict = await _context.Contracts.AnyAsync(c =>
            c.Id != contract.Id &&
            c.RoomId == contract.RoomId &&
            c.Status == ContractStatus.ACTIVE &&
            c.StartDate < contract.EndDate &&
            c.EndDate > contract.StartDate,
            cancellationToken);

        if (hasActiveConflict)
        {
            return Result.Failure<ContractDto>(new Error("Contract.RoomAlreadyLeased", "Phòng đã có hợp đồng thuê có hiệu lực trong khoảng thời gian này."));
        }

        try
        {
            contract.LandlordFinalize(landlordUserId);

            _context.ContractAuditLogs.Add(new ContractAuditLog
            {
                ContractId = contract.Id,
                Action = "LANDLORD_FINALIZED",
                PerformedByUserId = landlordUserId,
                PerformedByRole = "LANDLORD",
                Timestamp = DateTimeOffset.UtcNow,
                Notes = "Chủ nhà hoàn tất xác nhận và kích hoạt hợp đồng (ACTIVE).",
                TermsSnapshot = contract.TermsSnapshot
            });

            // Đồng bộ trạng thái phòng sang RENTED
            await _roomOwnershipService.SetRoomStatusAsync(contract.RoomId, "RENTED", cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(contract));
        }
        catch (Exception ex)
        {
            return Result.Failure<ContractDto>(new Error("Contract.FinalizationError", ex.Message));
        }
    }

    public async Task<Result<ContractDto>> GetContractByIdAsync(
        long currentUserId, 
        long contractId, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
        {
            return Result.Failure<ContractDto>(new Error("Contract.NotFound", "Không tìm thấy hợp đồng."));
        }

        // Phân quyền theo tài nguyên: Chỉ Landlord hoặc Tenant của hợp đồng mới được xem
        if (contract.LandlordId != currentUserId && contract.TenantId != currentUserId)
        {
            return Result.Failure<ContractDto>(new Error("Contract.Forbidden", "Bạn không có quyền truy cập hợp đồng này."));
        }

        return Result.Success(MapToDto(contract));
    }

    public async Task<Result<ContractDto>> CancelContractAsync(
        long currentUserId, 
        long contractId, 
        CancelContractRequest request, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(new Error("Contract.NotFound", "Không tìm thấy hợp đồng."));
        }

        try
        {
            contract.Cancel(currentUserId, request.Reason);

            _context.ContractAuditLogs.Add(new ContractAuditLog
            {
                ContractId = contract.Id,
                Action = "CANCELLED",
                PerformedByUserId = currentUserId,
                PerformedByRole = (currentUserId == contract.LandlordId) ? "LANDLORD" : "TENANT",
                Timestamp = DateTimeOffset.UtcNow,
                Notes = $"Hủy hợp đồng: {request.Reason}"
            });

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(contract));
        }
        catch (Exception ex)
        {
            return Result.Failure<ContractDto>(new Error("Contract.CancellationError", ex.Message));
        }
    }

    public async Task<Result<ContractDto>> TerminateContractAsync(
        long landlordUserId, 
        long contractId, 
        TerminateContractRequest request, 
        CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(new Error("Contract.NotFound", "Không tìm thấy hợp đồng."));
        }

        try
        {
            contract.Terminate(landlordUserId, request.Reason);

            _context.ContractAuditLogs.Add(new ContractAuditLog
            {
                ContractId = contract.Id,
                Action = "TERMINATED",
                PerformedByUserId = landlordUserId,
                PerformedByRole = "LANDLORD",
                Timestamp = DateTimeOffset.UtcNow,
                Notes = $"Thanh lý/Chấm dứt hợp đồng: {request.Reason}"
            });

            // Khi thanh lý hợp đồng, trả lại trạng thái AVAILABLE cho phòng nếu không còn hợp đồng ACTIVE khác
            var hasOtherActive = await _context.Contracts.AnyAsync(c =>
                c.Id != contract.Id &&
                c.RoomId == contract.RoomId &&
                c.Status == ContractStatus.ACTIVE,
                cancellationToken);

            if (!hasOtherActive)
            {
                await _roomOwnershipService.SetRoomStatusAsync(contract.RoomId, "AVAILABLE", cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(contract));
        }
        catch (Exception ex)
        {
            return Result.Failure<ContractDto>(new Error("Contract.TerminationError", ex.Message));
        }
    }

    #endregion

    private static ContractDto MapToDto(Contract c) => new(
        c.Id,
        c.ContractCode,
        c.RoomId,
        c.TenantId,
        c.LandlordId,
        c.StartDate,
        c.EndDate,
        c.MonthlyRent,
        c.DepositAmount,
        c.DepositStatus,
        c.BillingDueDay,
        c.ContractFileUrl,
        c.Status,
        c.TermsSnapshot,
        c.TenantConfirmedAt,
        c.LandlordConfirmedAt,
        c.RejectionReason,
        c.CancellationReason,
        c.TerminationReason,
        c.CreatedAt,
        c.UpdatedAt
    );
}
