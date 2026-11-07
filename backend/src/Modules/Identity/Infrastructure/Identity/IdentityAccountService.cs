using Microsoft.AspNetCore.Identity;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Modules.Identity.Domain.Enums;

namespace HomeMatch.Modules.Identity.Infrastructure.Identity;

public class IdentityAccountService : IIdentityAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public IdentityAccountService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<CreateUserResult> CreateUserAsync(string email, string password, string role, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Status = UserStatus.Active,
            EmailConfirmed = false,
            SecurityVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new CreateUserResult(false, 0, result.Errors.Select(e => e.Description));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new ApplicationRole(role));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            return new CreateUserResult(false, user.Id, roleResult.Errors.Select(e => e.Description));
        }

        return new CreateUserResult(true, user.Id, Array.Empty<string>());
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with id {userId} not found.");
        }

        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<OperationResult> ConfirmEmailAsync(long userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new OperationResult(false, new[] { "Không tìm thấy người dùng." });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return new OperationResult(false, result.Errors.Select(e => e.Description));
        }

        return new OperationResult(true, Array.Empty<string>());
    }

    public async Task<bool> CheckEmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

    public async Task<bool> IsEmailConfirmedAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null && user.EmailConfirmed;
    }

    public async Task<long?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task<VerifyCredentialsResult> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return new VerifyCredentialsResult(false, 0, string.Empty, string.Empty, UserStatus.Suspended, false, 0, IdentityErrors.InvalidCredentials);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return new VerifyCredentialsResult(false, user.Id, user.Email ?? string.Empty, string.Empty, user.Status, user.EmailConfirmed, user.SecurityVersion, IdentityErrors.InvalidCredentials);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? AccountRoles.Tenant;

        return new VerifyCredentialsResult(
            Succeeded: true,
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            Role: primaryRole,
            Status: user.Status,
            EmailConfirmed: user.EmailConfirmed,
            SecurityVersion: user.SecurityVersion);
    }

    public async Task<UserAccountDetails?> GetUserDetailsByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? AccountRoles.Tenant;

        return new UserAccountDetails(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            Role: primaryRole,
            Status: user.Status,
            EmailConfirmed: user.EmailConfirmed,
            SecurityVersion: user.SecurityVersion,
            PhoneNumber: user.PhoneNumber,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt);
    }

    public async Task<int> IncrementSecurityVersionAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with id {userId} not found.");
        }

        user.SecurityVersion++;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _userManager.UpdateAsync(user);
        return user.SecurityVersion;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with id {userId} not found.");
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<OperationResult> ResetPasswordAsync(long userId, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new OperationResult(false, new[] { "Không tìm thấy người dùng." });
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return new OperationResult(false, result.Errors.Select(e => e.Description));
        }

        return new OperationResult(true, Array.Empty<string>());
    }
}
