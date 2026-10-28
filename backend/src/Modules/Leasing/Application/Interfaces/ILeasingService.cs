using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Interfaces;

public interface ILeasingService
{
    // UC3.7.1 - Quản lý khách quan tâm (Leads)
    Task<Result<IEnumerable<LeadDto>>> GetLandlordLeadsAsync(
        long landlordUserId, 
        long? listingId = null, 
        LeadStatus? status = null, 
        CancellationToken cancellationToken = default);

    Task<Result<LeadDto>> GetLeadByIdAsync(
        long landlordUserId, 
        long leadId, 
        CancellationToken cancellationToken = default);

    Task<Result<LeadDto>> UpdateLeadStatusAsync(
        long landlordUserId, 
        long leadId, 
        UpdateLeadRequest request, 
        CancellationToken cancellationToken = default);

    // UC3.7.2 - Quản lý danh sách người thuê (Tenants)
    Task<Result<IEnumerable<TenantSummaryDto>>> GetLandlordTenantsAsync(
        long landlordUserId, 
        long? roomId = null, 
        TenantLifecycleStatus? statusFilter = null, 
        CancellationToken cancellationToken = default);

    Task<Result<TenantSummaryDto>> GetTenantDetailAsync(
        long landlordUserId, 
        long contractId, 
        CancellationToken cancellationToken = default);

    // UC3.7.3 - Soạn và quản lý hợp đồng thuê (Contracts)
    Task<Result<ContractDto>> CreateContractDraftAsync(
        long landlordUserId, 
        CreateContractDraftRequest request, 
        CancellationToken cancellationToken = default);

    Task<Result<ContractDto>> UpdateContractDraftAsync(
        long landlordUserId, 
        long contractId, 
        UpdateContractDraftRequest request, 
        CancellationToken cancellationToken = default);

    Task<Result<ContractDto>> SendContractToTenantAsync(
        long landlordUserId, 
        long contractId, 
        CancellationToken cancellationToken = default);

    Task<Result<ContractDto>> TenantConfirmOrRejectContractAsync(
        long tenantUserId, 
        long contractId, 
        TenantConfirmContractRequest request, 
        CancellationToken cancellationToken = default);

    Task<Result<ContractDto>> LandlordFinalizeContractAsync(
        long landlordUserId, 
        long contractId, 
        CancellationToken cancellationToken = default);

    Task<Result<ContractDto>> GetContractByIdAsync(
        long currentUserId, 
        long contractId, 
        CancellationToken cancellationToken = default);

    Task<Result<ContractDto>> CancelContractAsync(
        long currentUserId, 
        long contractId, 
        CancelContractRequest request, 
        CancellationToken cancellationToken = default);

    Task<Result<ContractDto>> TerminateContractAsync(
        long landlordUserId, 
        long contractId, 
        TerminateContractRequest request, 
        CancellationToken cancellationToken = default);
}
