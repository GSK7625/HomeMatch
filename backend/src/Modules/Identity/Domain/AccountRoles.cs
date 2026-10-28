namespace HomeMatch.Modules.Identity.Domain;

public static class AccountRoles
{
    public const string Tenant = "Tenant";
    public const string Landlord = "Landlord";
    public const string Admin = "Admin";

    public static IReadOnlyList<string> All { get; } =
        Array.AsReadOnly(new[] { Tenant, Landlord, Admin });

    public static bool CanSelfRegister(string? role) => role is Tenant or Landlord;
}