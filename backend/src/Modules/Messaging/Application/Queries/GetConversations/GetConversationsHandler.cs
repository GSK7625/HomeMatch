using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Messaging.Application.Queries.GetConversations;

public sealed class GetConversationsHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;

    public GetConversationsHandler(
        IConversationRepository conversationRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader)
    {
        _conversationRepository = conversationRepository;
        _listingReader = listingReader;
        _userReader = userReader;
    }

    public async Task<Result<PagedConversationsDto>> HandleAsync(
        GetConversationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (conversations, totalCount) = await _conversationRepository.GetPagedByUserAsync(
            query.UserId,
            page,
            pageSize,
            cancellationToken);

        if (conversations.Count == 0)
        {
            return Result.Success(new PagedConversationsDto([], page, pageSize, totalCount));
        }

        var conversationIds = conversations.Select(c => c.Id).ToList();
        var listingIds = conversations
            .Where(c => c.ListingId.HasValue)
            .Select(c => c.ListingId!.Value)
            .Distinct()
            .ToList();

        var listingMap = listingIds.Count > 0
            ? await _listingReader.GetListingsByIdsAsync(listingIds, cancellationToken)
            : new Dictionary<long, ListingContractInfo>();

        var lastMessageMap = await _conversationRepository.GetLastMessagesByConversationIdsAsync(
            conversationIds,
            cancellationToken);

        var unreadMap = await _conversationRepository.GetUnreadCountsByConversationIdsAsync(
            conversationIds,
            query.UserId,
            cancellationToken);

        var userCache = new Dictionary<long, IdentityUserInfo?>();
        async Task<IdentityUserInfo?> GetUserCachedAsync(long userId)
        {
            if (userId <= 0) return null;
            if (userCache.TryGetValue(userId, out var cached)) return cached;
            var userInfo = await _userReader.GetUserByIdAsync(userId, cancellationToken);
            userCache[userId] = userInfo;
            return userInfo;
        }

        var dtos = new List<ConversationDto>(conversations.Count);
        foreach (var conv in conversations)
        {
            ListingContractInfo? listing = null;
            if (conv.ListingId.HasValue)
            {
                listingMap.TryGetValue(conv.ListingId.Value, out listing);
            }

            lastMessageMap.TryGetValue(conv.Id, out var lastMsg);
            unreadMap.TryGetValue(conv.Id, out var unreadCount);

            var initiator = await GetUserCachedAsync(conv.InitiatorId);
            var recipient = await GetUserCachedAsync(conv.RecipientId);

            dtos.Add(MessagingMapper.ToConversationDto(
                conv,
                query.UserId,
                listing,
                initiator,
                recipient,
                lastMsg,
                unreadCount));
        }

        return Result.Success(new PagedConversationsDto(dtos, page, pageSize, totalCount));
    }
}
