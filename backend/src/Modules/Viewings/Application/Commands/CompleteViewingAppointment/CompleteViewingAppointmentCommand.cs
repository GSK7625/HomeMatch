namespace HomeMatch.Modules.Viewings.Application.Commands.CompleteViewingAppointment;

public record CompleteViewingAppointmentCommand(
    long AppointmentId,
    long ActorUserId);
