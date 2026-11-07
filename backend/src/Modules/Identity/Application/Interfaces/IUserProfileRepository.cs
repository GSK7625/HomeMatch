using HomeMatch.Modules.Identity.Domain.Entities;

namespace HomeMatch.Modules.Identity.Application.Interfaces;

public interface IUserProfileRepository
{
    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
