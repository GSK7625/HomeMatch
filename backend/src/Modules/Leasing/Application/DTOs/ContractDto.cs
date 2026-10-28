using HomeMatch.Modules.Leasing.Domain.Enums;

namespace HomeMatch.Modules.Leasing.Application.DTOs;

public record ContractDto(
    long Id,
    string ContractCode,
    long RoomId,
    long TenantId,
    long LandlordId,
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    DepositStatus DepositStatus,
    int BillingDueDay,
    string? ContractFileUrl,
    ContractStatus Status,
    string? TermsSnapshot,
    DateTimeOffset? TenantConfirmedAt,
    DateTimeOffset? LandlordConfirmedAt,
    string? RejectionReason,
    string? CancellationReason,
    string? TerminationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record CreateContractDraftRequest(
    long RoomId,
    long TenantId,
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    int BillingDueDay = 5,
    string? TermsSnapshot = null
);

public record UpdateContractDraftRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    int BillingDueDay = 5,
    string? TermsSnapshot = null
);

public record TenantConfirmContractRequest(
    string TermsSnapshotToVerify,
    bool Accept,
    string? RejectionReason = null
);

public record CancelContractRequest(
    string Reason
);

public record TerminateContractRequest(
    string Reason
);
