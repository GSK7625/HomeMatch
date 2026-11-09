using HomeMatch.Modules.Viewings.Domain.Enums;

namespace HomeMatch.Modules.Viewings.Domain.Entities;

public class ViewingAppointment
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long ListingId { get; set; }
    public DateTimeOffset AppointmentTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? Notes { get; set; }
    public string Status { get; set; } = ViewingAppointmentStatus.Pending;
    public string? CancelledBy { get; set; }
    public string? CancelledReason { get; set; }
    public DateTimeOffset? RemindAt { get; set; }
    public bool IsReminded { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public VirtualViewing? VirtualViewing { get; set; }
}
