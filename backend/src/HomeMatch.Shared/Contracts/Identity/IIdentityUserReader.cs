namespace HomeMatch.Shared.Contracts.Identity;

public interface IIdentityUserReader
{
    Task<IdentityUserInfo?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);
}
