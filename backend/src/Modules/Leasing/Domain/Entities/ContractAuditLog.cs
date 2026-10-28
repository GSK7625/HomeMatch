namespace HomeMatch.Modules.Leasing.Domain.Entities;

public class ContractAuditLog
{
    public long Id { get; set; }
    public long ContractId { get; set; }
    public string Action { get; set; } = string.Empty;
    public long PerformedByUserId { get; set; }
    public string PerformedByRole { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
    public string? TermsSnapshot { get; set; }
}
