namespace HomeMatch.Modules.Viewings.Application.Commands.ConfirmViewingAppointment;

public record ConfirmViewingAppointmentCommand(
    long AppointmentId,
    long ActorUserId,
    string? VirtualRoomUrl = null);
