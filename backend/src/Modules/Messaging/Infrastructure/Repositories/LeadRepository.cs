using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Messaging.Infrastructure.Repositories;

public sealed class LeadRepository : ILeadRepository
{
    private readonly MessagingDbContext _dbContext;

    public LeadRepository(MessagingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Lead>> GetByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
        {
            return [];
        }

        var idSet = listingIds.Distinct().ToList();

        return await _dbContext.Leads
            .AsNoTracking()
            .Where(l => idSet.Contains(l.ListingId))
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lead?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Leads
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Dictionary<(long TenantId, long ListingId), long>> GetConversationMapByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        long landlordId,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
        {
            return new Dictionary<(long TenantId, long ListingId), long>();
        }

        var idSet = listingIds.Distinct().ToList();

        var conversations = await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.ListingId.HasValue
                        && idSet.Contains(c.ListingId.Value)
                        && (c.InitiatorId == landlordId || c.RecipientId == landlordId))
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                c.Id,
                ListingId = c.ListingId!.Value,
                TenantId = c.InitiatorId == landlordId ? c.RecipientId : c.InitiatorId
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<(long TenantId, long ListingId), long>();
        foreach (var conv in conversations)
        {
            var key = (conv.TenantId, conv.ListingId);
            result.TryAdd(key, conv.Id);
        }

        return result;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
