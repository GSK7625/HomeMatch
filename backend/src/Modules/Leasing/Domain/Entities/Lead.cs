using HomeMatch.Modules.Leasing.Domain.Enums;

namespace HomeMatch.Modules.Leasing.Domain.Entities;

public class Lead
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long ListingId { get; set; }
    public long? RoomId { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.NEW;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public void UpdateStatus(LeadStatus newStatus, string? note = null)
    {
        // Business validation on status transitions
        if (Status == LeadStatus.CLOSED && newStatus != LeadStatus.NEW)
        {
            throw new InvalidOperationException("Không thể mở lại trạng thái của khách quan tâm đã đóng trừ khi tạo mới.");
        }

        Status = newStatus;
        if (!string.IsNullOrWhiteSpace(note))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) 
                ? $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}] {note}"
                : $"{Notes}\n[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}] {note}";
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
