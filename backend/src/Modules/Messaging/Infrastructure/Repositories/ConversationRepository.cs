using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Modules.Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Messaging.Infrastructure.Repositories;

internal sealed class ConversationRepository : IConversationRepository
{
    private readonly MessagingDbContext _dbContext;

    public ConversationRepository(MessagingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetListingConversationAsync(
        long userAId,
        long userBId,
        long listingId,
        CancellationToken cancellationToken = default)
    {
        var minId = Math.Min(userAId, userBId);
        var maxId = Math.Max(userAId, userBId);

        return await _dbContext.Conversations
            .FirstOrDefaultAsync(
                c => c.ListingId == listingId
                     && c.UserMinId == minId
                     && c.UserMaxId == maxId,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<Conversation> Items, int TotalCount)> GetPagedByUserAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.InitiatorId == userId || c.RecipientId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Message> Items, int TotalCount)> GetMessagesPagedAsync(
        long conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<long, Message>> GetLastMessagesByConversationIdsAsync(
        IReadOnlyCollection<long> conversationIds,
        CancellationToken cancellationToken = default)
    {
        if (conversationIds.Count == 0)
        {
            return new Dictionary<long, Message>();
        }

        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => conversationIds.Contains(m.ConversationId))
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<long, Message>();
        foreach (var msg in messages)
        {
            result.TryAdd(msg.ConversationId, msg);
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<long, int>> GetUnreadCountsByConversationIdsAsync(
        IReadOnlyCollection<long> conversationIds,
        long recipientUserId,
        CancellationToken cancellationToken = default)
    {
        if (conversationIds.Count == 0)
        {
            return new Dictionary<long, int>();
        }

        var counts = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => conversationIds.Contains(m.ConversationId)
                        && m.SenderId != recipientUserId
                        && !m.IsRead)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.ConversationId, x => x.Count);
    }

    public async Task<int> MarkMessagesAsReadAsync(
        long conversationId,
        long readerUserId,
        CancellationToken cancellationToken = default)
    {
        var unreadMessages = await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != readerUserId
                        && !m.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadMessages.Count == 0)
        {
            return 0;
        }

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return unreadMessages.Count;
    }

    public async Task<MessageDraft?> GetDraftAsync(
        long conversationId,
        long authorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MessageDrafts
            .FirstOrDefaultAsync(
                d => d.ConversationId == conversationId && d.AuthorId == authorId,
                cancellationToken);
    }

    public async Task<MessageDraft> UpsertDraftAsync(
        long conversationId,
        long authorId,
        string content,
        string source,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetDraftAsync(conversationId, authorId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var draft = new MessageDraft
            {
                ConversationId = conversationId,
                AuthorId = authorId,
                Content = content,
                Source = source,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _dbContext.MessageDrafts.AddAsync(draft, cancellationToken);
            return draft;
        }

        existing.Content = content;
        existing.Source = source;
        existing.UpdatedAt = now;
        return existing;
    }

    public async Task DeleteDraftAsync(
        long conversationId,
        long authorId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetDraftAsync(conversationId, authorId, cancellationToken);
        if (existing is not null)
        {
            _dbContext.MessageDrafts.Remove(existing);
        }
    }

    public async Task EnsureLeadExistsAsync(
        long tenantId,
        long listingId,
        string source,
        CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Leads
            .AnyAsync(l => l.TenantId == tenantId && l.ListingId == listingId, cancellationToken);

        if (!exists)
        {
            await _dbContext.Leads.AddAsync(new Lead
            {
                TenantId = tenantId,
                ListingId = listingId,
                Status = LeadStatus.Contacted,
                Source = source,
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    public async Task<int> CountConversationsByUserAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .AsNoTracking()
            .CountAsync(c => c.InitiatorId == userId || c.RecipientId == userId, cancellationToken);
    }

    public async Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Conversations.AddAsync(conversation, cancellationToken);
    }

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _dbContext.Messages.AddAsync(message, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
