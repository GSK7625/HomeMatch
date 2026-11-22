using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Identity;

internal sealed class UserNotificationPreferenceReader : IUserNotificationPreferenceReader
{
    private readonly IUserProfileRepository _profileRepository;

    public UserNotificationPreferenceReader(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<bool> IsInAppNotificationEnabledAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        return profile?.NotifyPush ?? true;
    }
}
