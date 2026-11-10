using Microsoft.AspNetCore.Identity;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Identity;

internal sealed class IdentityOtpService : IIdentityOtpService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityOtpService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string> GenerateSignatureOtpAsync(long userId, long contractId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException($"Không tìm thấy tài khoản người dùng với Id {userId}.");
        }

        // Tạo mã OTP 6 chữ số TOTP bằng DefaultEmailProvider ("Email") của ASP.NET Core Identity
        return await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, $"SignContract:{contractId}");
    }

    public async Task<bool> VerifySignatureOtpAsync(long userId, long contractId, string otp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(otp))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        // Xác thực mã OTP bằng DefaultEmailProvider của ASP.NET Core Identity
        return await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, $"SignContract:{contractId}", otp.Trim());
    }
}
