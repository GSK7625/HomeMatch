using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Messaging.Application.Queries.GetConversationMessages;

public sealed class GetConversationMessagesHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;
    private readonly IChatRealtimeNotifier _realtimeNotifier;

    public GetConversationMessagesHandler(
        IConversationRepository conversationRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader,
        IChatRealtimeNotifier realtimeNotifier)
    {
        _conversationRepository = conversationRepository;
        _listingReader = listingReader;
        _userReader = userReader;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<Result<PagedMessagesDto>> HandleAsync(
        GetConversationMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var conversation = await _conversationRepository.GetByIdAsync(query.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure<PagedMessagesDto>(MessagingErrors.ConversationNotFound);
        }

        if (conversation.InitiatorId != query.ActorUserId
            && conversation.RecipientId != query.ActorUserId)
        {
            return Result.Failure<PagedMessagesDto>(MessagingErrors.Forbidden);
        }

        // Tự động đánh dấu tin nhắn đối phương gửi là đã đọc khi người dùng mở lịch sử chat
        var markedCount = await _conversationRepository.MarkMessagesAsReadAsync(
            conversation.Id,
            query.ActorUserId,
            cancellationToken);

        if (markedCount > 0)
        {
            var partnerUserId = conversation.InitiatorId == query.ActorUserId
                ? conversation.RecipientId
                : conversation.InitiatorId;

            await _realtimeNotifier.NotifyMessagesReadAsync(
                conversation.Id,
                query.ActorUserId,
                partnerUserId,
                markedCount,
                cancellationToken);
        }

        var (messages, totalCount) = await _conversationRepository.GetMessagesPagedAsync(
            conversation.Id,
            page,
            pageSize,
            cancellationToken);

        var listing = conversation.ListingId.HasValue
            ? await _listingReader.GetListingByIdAsync(conversation.ListingId.Value, cancellationToken)
            : null;

        var initiator = await _userReader.GetUserByIdAsync(conversation.InitiatorId, cancellationToken);
        var recipient = await _userReader.GetUserByIdAsync(conversation.RecipientId, cancellationToken);

        var lastMessage = messages.FirstOrDefault();
        var conversationDto = MessagingMapper.ToConversationDto(
            conversation,
            query.ActorUserId,
            listing,
            initiator,
            recipient,
            lastMessage,
            unreadCount: 0);

        var messageDtos = messages
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Select(m =>
            {
                var sender = m.SenderId == conversation.InitiatorId ? initiator : recipient;
                return MessagingMapper.ToMessageDto(m, sender);
            })
            .ToList();

        return Result.Success(new PagedMessagesDto(
            Conversation: conversationDto,
            Items: messageDtos,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount));
    }
}
