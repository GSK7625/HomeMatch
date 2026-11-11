namespace HomeMatch.Shared.Common;

public static class BusinessDate
{
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public static DateOnly Today() => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTimeZone).DateTime);
}
