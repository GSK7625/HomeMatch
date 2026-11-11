namespace HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointmentById;

public record GetViewingAppointmentByIdQuery(
    long AppointmentId,
    long ActorUserId);
