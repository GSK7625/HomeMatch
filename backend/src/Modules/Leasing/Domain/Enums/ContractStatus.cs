namespace HomeMatch.Modules.Leasing.Domain.Enums;

public static class ContractStatus
{
    public const string Draft = "DRAFT";
    public const string PendingSignature = "PENDING_SIGNATURE";
    public const string SignatureExpired = "SIGNATURE_EXPIRED";
    public const string Rejected = "REJECTED";
    public const string Signed = "SIGNED";
    public const string Active = "ACTIVE";
    public const string ExpiredPendingHandover = "EXPIRED_PENDING_HANDOVER";
    public const string HandoverOverdue = "HANDOVER_OVERDUE";
    public const string Completed = "COMPLETED";
    public const string Terminated = "TERMINATED";
    public const string Cancelled = "CANCELLED";
}
