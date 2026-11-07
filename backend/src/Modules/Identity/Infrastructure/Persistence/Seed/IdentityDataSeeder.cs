using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
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

    public static async Task SeedDefaultUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("IdentityDataSeeder");

        var defaultUsers = new[]
        {
            new
            {
                Email = "admin@homematch.vn",
                Password = "Password123@",
                Role = AccountRoles.Admin,
                FullName = "Quản Trị Viên Hệ Thống",
                PhoneNumber = "0901000001",
                AvatarUrl = "https://assets.homematch.vn/avatars/admin.png",
                Gender = "OTHER"
            },
            new
            {
                Email = "landlord@homematch.vn",
                Password = "Password123@",
                Role = AccountRoles.Landlord,
                FullName = "Nguyễn Văn Hoàng (Chủ Trọ)",
                PhoneNumber = "0902000002",
                AvatarUrl = "https://assets.homematch.vn/avatars/landlord_hoang.png",
                Gender = "MALE"
            },
            new
            {
                Email = "tenant@homematch.vn",
                Password = "Password123@",
                Role = AccountRoles.Tenant,
                FullName = "Trần Bình An (Người Thuê)",
                PhoneNumber = "0903000003",
                AvatarUrl = "https://assets.homematch.vn/avatars/tenant_an.png",
                Gender = "MALE"
            }
        };

        foreach (var def in defaultUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(def.Email);
            if (existingUser == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = def.Email,
                    Email = def.Email,
                    PhoneNumber = def.PhoneNumber,
                    PhoneNumberConfirmed = true,
                    EmailConfirmed = true,
                    Status = UserStatus.Active,
                    SecurityVersion = 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(newUser, def.Password);
                if (createResult.Succeeded)
                {
                    if (!await roleManager.RoleExistsAsync(def.Role))
                    {
                        await roleManager.CreateAsync(new ApplicationRole(def.Role));
                    }

                    await userManager.AddToRoleAsync(newUser, def.Role);

                    // Khởi tạo hồ sơ người dùng (UserProfile)
                    var existingProfile = await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == newUser.Id);
                    if (existingProfile == null)
                    {
                        dbContext.UserProfiles.Add(new UserProfile
                        {
                            UserId = newUser.Id,
                            FullName = def.FullName,
                            AvatarUrl = def.AvatarUrl,
                            Gender = def.Gender,
                            UpdatedAt = DateTimeOffset.UtcNow
                        });
                        await dbContext.SaveChangesAsync();
                    }

                    logger?.LogInformation("✅ Đã khởi tạo tài khoản mẫu thành công: {Email} (Vai trò: {Role})", def.Email, def.Role);
                }
                else
                {
                    logger?.LogError("❌ Không thể tạo tài khoản mẫu {Email}: {Errors}",
                        def.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Đảm bảo tài khoản đã được kích hoạt và xác thực email
                bool updated = false;
                if (!existingUser.EmailConfirmed)
                {
                    existingUser.EmailConfirmed = true;
                    updated = true;
                }
                if (existingUser.Status != UserStatus.Active)
                {
                    existingUser.Status = UserStatus.Active;
                    updated = true;
                }
                if (updated)
                {
                    await userManager.UpdateAsync(existingUser);
                }

                // Đảm bảo UserProfile đã tồn tại
                var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == existingUser.Id);
                if (profile == null)
                {
                    dbContext.UserProfiles.Add(new UserProfile
                    {
                        UserId = existingUser.Id,
                        FullName = def.FullName,
                        AvatarUrl = def.AvatarUrl,
                        Gender = def.Gender,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
