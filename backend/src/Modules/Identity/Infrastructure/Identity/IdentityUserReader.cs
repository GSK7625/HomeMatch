using Microsoft.AspNetCore.Identity;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Identity;

internal sealed class IdentityUserReader : IIdentityUserReader
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserProfileRepository _profileRepository;

    public IdentityUserReader(
        UserManager<ApplicationUser> userManager,
        IUserProfileRepository profileRepository)
    {
        _userManager = userManager;
        _profileRepository = profileRepository;
    }

    public async Task<IdentityUserInfo?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        var roles = await _userManager.GetRolesAsync(user);

        return new IdentityUserInfo(
            Id: user.Id,
            FullName: profile?.FullName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            PhoneNumber: user.PhoneNumber,
            Role: roles.FirstOrDefault()
        );
    }
}
