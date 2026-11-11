namespace HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointments;

public record GetViewingAppointmentsQuery(
    long UserId,
    string? RoleView,
    string? Status,
    int Page = 1,
    int PageSize = 20);
