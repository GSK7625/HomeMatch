namespace HomeMatch.Modules.Viewings.Application.Commands.CancelViewingAppointment;

public record CancelViewingAppointmentCommand(
    long AppointmentId,
    long ActorUserId,
    string? CancelledReason);
