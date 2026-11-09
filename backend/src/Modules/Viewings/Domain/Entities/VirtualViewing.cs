using HomeMatch.Modules.Viewings.Domain.Enums;

namespace HomeMatch.Modules.Viewings.Domain.Entities;

public class VirtualViewing
{
    public long Id { get; set; }
    public long AppointmentId { get; set; }
    public string RoomUrl { get; set; } = string.Empty;
    public string Status { get; set; } = VirtualViewingStatus.Scheduled;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ViewingAppointment? Appointment { get; set; }
}
