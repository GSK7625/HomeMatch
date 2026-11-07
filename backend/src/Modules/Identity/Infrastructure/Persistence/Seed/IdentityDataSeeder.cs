using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Infrastructure.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Seed;

public static class IdentityDataSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("IdentityDataSeeder");

        foreach (var roleName in AccountRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName);
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger?.LogInformation("Đã khởi tạo vai trò mặc định: {RoleName}", roleName);
                }
                else
                {
                    logger?.LogError("Không thể tạo vai trò {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
