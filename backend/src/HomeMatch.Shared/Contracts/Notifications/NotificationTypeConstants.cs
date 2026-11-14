namespace HomeMatch.Shared.Contracts.Notifications;

public static class NotificationTypeConstants
{
    public const string ContractPendingSignature = "CONTRACT_PENDING_SIGNATURE";
    public const string ContractSigned = "CONTRACT_SIGNED";
    public const string ContractSignatureExpired = "CONTRACT_SIGNATURE_EXPIRED";
    public const string ContractActive = "CONTRACT_ACTIVE";
    public const string ContractExpiring = "CONTRACT_EXPIRING";
    public const string ContractExpiredPendingHandover = "CONTRACT_EXPIRED_PENDING_HANDOVER";
    public const string ContractHandoverOverdue = "CONTRACT_HANDOVER_OVERDUE";
}
