namespace HomeMatch.Modules.Identity.Domain.Constants;

public static class AccountRoles
{
    public const string Tenant = "Tenant";
    public const string Landlord = "Landlord";
    public const string Admin = "Admin";

    public static readonly IReadOnlySet<string> PublicAssignableRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Tenant,
        Landlord
    };

    public static readonly IReadOnlyList<string> All =
    [
        Tenant,
        Landlord,
        Admin
    ];
}
