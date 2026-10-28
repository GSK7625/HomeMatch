using HomeMatch.Modules.Leasing.Domain.Enums;

namespace HomeMatch.Modules.Leasing.Application.DTOs;

public record TenantSummaryDto(
    long TenantId,
    long ContractId,
    string ContractCode,
    long RoomId,
    long LandlordId,
    DateOnly StartDate,
    DateOnly EndDate,
    long MonthlyRent,
    long DepositAmount,
    DepositStatus DepositStatus,
    ContractStatus ContractStatus,
    TenantLifecycleStatus LifecycleStatus
);
