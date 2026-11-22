namespace HomeMatch.Shared.Contracts.Identity;

public interface IUserNotificationPreferenceReader
{
    Task<bool> IsInAppNotificationEnabledAsync(long userId, CancellationToken cancellationToken = default);
}
