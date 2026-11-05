using Microsoft.AspNetCore.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<long>
{
    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
