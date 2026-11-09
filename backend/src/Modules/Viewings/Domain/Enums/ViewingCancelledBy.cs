namespace HomeMatch.Modules.Viewings.Domain.Enums;

public static class ViewingCancelledBy
{
    public const string Tenant = "TENANT";
    public const string Landlord = "LANDLORD";
    public const string System = "SYSTEM";

    public static readonly string[] All = [Tenant, Landlord, System];
}
