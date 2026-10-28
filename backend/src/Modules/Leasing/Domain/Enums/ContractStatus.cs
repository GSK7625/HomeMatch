namespace HomeMatch.Modules.Leasing.Domain.Enums;

public enum ContractStatus
{
    DRAFT = 1,
    PENDING_TENANT_CONFIRMATION = 2,
    PENDING_LANDLORD_CONFIRMATION = 3,
    ACTIVE = 4,
    COMPLETED = 5,
    TERMINATED = 6,
    REJECTED = 7,
    CANCELLED = 8
}
