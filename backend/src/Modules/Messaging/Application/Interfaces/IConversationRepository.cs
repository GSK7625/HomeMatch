using HomeMatch.Modules.Messaging.Domain.Entities;

namespace HomeMatch.Modules.Messaging.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Conversation?> GetListingConversationAsync(
        long userAId,
        long userBId,
        long listingId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Conversation> Items, int TotalCount)> GetPagedByUserAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Message> Items, int TotalCount)> GetMessagesPagedAsync(
        long conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<long, Message>> GetLastMessagesByConversationIdsAsync(
        IReadOnlyCollection<long> conversationIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<long, int>> GetUnreadCountsByConversationIdsAsync(
        IReadOnlyCollection<long> conversationIds,
        long recipientUserId,
        CancellationToken cancellationToken = default);

    Task<int> MarkMessagesAsReadAsync(
        long conversationId,
        long readerUserId,
        CancellationToken cancellationToken = default);

    Task<MessageDraft?> GetDraftAsync(
        long conversationId,
        long authorId,
        CancellationToken cancellationToken = default);

    Task<MessageDraft> UpsertDraftAsync(
        long conversationId,
        long authorId,
        string content,
        string source,
        CancellationToken cancellationToken = default);

    Task DeleteDraftAsync(
        long conversationId,
        long authorId,
        CancellationToken cancellationToken = default);

    Task EnsureLeadExistsAsync(
        long tenantId,
        long listingId,
        string source,
        CancellationToken cancellationToken = default);

    Task<int> CountConversationsByUserAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);

    Task AddMessageAsync(Message message, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
