namespace HomeMatch.Modules.Viewings.Domain.Enums;

public static class ViewingAppointmentStatus
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Cancelled = "CANCELLED";
    public const string Completed = "COMPLETED";

    public static readonly string[] All = [Pending, Confirmed, Cancelled, Completed];
}
