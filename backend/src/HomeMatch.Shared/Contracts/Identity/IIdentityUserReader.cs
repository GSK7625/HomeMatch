namespace HomeMatch.Shared.Contracts.Identity;

public interface IIdentityUserReader
{
    Task<IdentityUserInfo?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityUserInfo>> GetTenantsAsync(CancellationToken cancellationToken = default);
}
