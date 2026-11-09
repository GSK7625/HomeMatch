namespace HomeMatch.Modules.Viewings.Application.Commands.BookViewingAppointment;

public record BookViewingAppointmentCommand(
    long TenantId,
    long ListingId,
    DateTimeOffset AppointmentTime,
    int DurationMinutes = 30,
    string? Notes = null,
    bool IsVirtualViewing = false);
