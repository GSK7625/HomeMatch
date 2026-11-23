using HomeMatch.Modules.Messaging.Domain.Entities;

namespace HomeMatch.Modules.Messaging.Application.Interfaces;

public interface ILeadRepository
{
    Task<IReadOnlyList<Lead>> GetByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        CancellationToken cancellationToken = default);

    Task<Lead?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Dictionary<(long TenantId, long ListingId), long>> GetConversationMapByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        long landlordId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
