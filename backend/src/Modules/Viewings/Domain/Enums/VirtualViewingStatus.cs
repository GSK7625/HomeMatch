namespace HomeMatch.Modules.Viewings.Domain.Enums;

public static class VirtualViewingStatus
{
    public const string Scheduled = "SCHEDULED";
    public const string InProgress = "IN_PROGRESS";
    public const string Ended = "ENDED";
    public const string Cancelled = "CANCELLED";

    public static readonly string[] All = [Scheduled, InProgress, Ended, Cancelled];
}
