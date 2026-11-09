using HomeMatch.Modules.Messaging.Domain.Enums;

namespace HomeMatch.Modules.Messaging.Domain.Entities;

public class Lead
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long ListingId { get; set; }
    public string Status { get; set; } = LeadStatus.New;
    public string? Notes { get; set; }
    public string Source { get; set; } = LeadSource.Direct;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
