using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Messaging.Application.Commands.StartOrGetConversation;

public sealed class StartOrGetConversationHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;
    private readonly IChatRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<StartOrGetConversationHandler> _logger;

    public StartOrGetConversationHandler(
        IConversationRepository conversationRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader,
        IChatRealtimeNotifier realtimeNotifier,
        ILogger<StartOrGetConversationHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _listingReader = listingReader;
        _userReader = userReader;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task<Result<ConversationDto>> HandleAsync(
        StartOrGetConversationCommand command,
        CancellationToken cancellationToken = default)
    {
        var listing = await _listingReader.GetListingByIdAsync(command.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure<ConversationDto>(MessagingErrors.ListingNotFound);
        }

        var recipientUserId = command.RecipientUserId ?? listing.LandlordId;

        if (command.InitiatorUserId == recipientUserId)
        {
            return Result.Failure<ConversationDto>(MessagingErrors.SelfConversationNotAllowed);
        }

        // Tuân thủ trg_validate_conversation_parties: một trong hai người phải là chủ sở hữu tin đăng
        if (command.InitiatorUserId != listing.LandlordId && recipientUserId != listing.LandlordId)
        {
            return Result.Failure<ConversationDto>(MessagingErrors.InvalidListingParticipant);
        }

        var now = DateTimeOffset.UtcNow;
        var conversation = await _conversationRepository.GetListingConversationAsync(
            command.InitiatorUserId,
            recipientUserId,
            command.ListingId,
            cancellationToken);

        var isNewConversation = false;
        if (conversation is null)
        {
            isNewConversation = true;
            conversation = new Conversation
            {
                InitiatorId = command.InitiatorUserId,
                RecipientId = recipientUserId,
                ListingId = command.ListingId,
                ConversationType = ConversationType.Listing,
                LastMessageAt = now,
                CreatedAt = now
            };
            await _conversationRepository.AddConversationAsync(conversation, cancellationToken);
        }

        var tenantId = command.InitiatorUserId == listing.LandlordId
            ? recipientUserId
            : command.InitiatorUserId;

        await _conversationRepository.EnsureLeadExistsAsync(
            tenantId,
            command.ListingId,
            LeadSource.Direct,
            cancellationToken);

        await _conversationRepository.SaveChangesAsync(cancellationToken);

        var initiator = await _userReader.GetUserByIdAsync(conversation.InitiatorId, cancellationToken);
        var recipient = await _userReader.GetUserByIdAsync(conversation.RecipientId, cancellationToken);

        Message? lastMessage = null;
        if (!string.IsNullOrWhiteSpace(command.InitialMessage))
        {
            lastMessage = new Message
            {
                ConversationId = conversation.Id,
                SenderId = command.InitiatorUserId,
                MessageType = MessageType.Text,
                Content = command.InitialMessage.Trim(),
                IsRead = false,
                CreatedAt = now
            };
            conversation.LastMessageAt = now;
            await _conversationRepository.AddMessageAsync(lastMessage, cancellationToken);
            await _conversationRepository.SaveChangesAsync(cancellationToken);

            var senderInfo = command.InitiatorUserId == conversation.InitiatorId ? initiator : recipient;
            var messageDto = MessagingMapper.ToMessageDto(lastMessage, senderInfo);
            await _realtimeNotifier.NotifyMessageSentAsync(
                conversation.InitiatorId,
                conversation.RecipientId,
                messageDto,
                cancellationToken);
        }
        else if (!isNewConversation)
        {
            var lastMap = await _conversationRepository.GetLastMessagesByConversationIdsAsync(
                [conversation.Id],
                cancellationToken);
            lastMap.TryGetValue(conversation.Id, out lastMessage);
        }

        var unreadMap = await _conversationRepository.GetUnreadCountsByConversationIdsAsync(
            [conversation.Id],
            command.InitiatorUserId,
            cancellationToken);
        unreadMap.TryGetValue(conversation.Id, out var unreadCount);

        _logger.LogInformation(
            "Người dùng {UserId} đã mở hội thoại #{ConversationId} cho tin đăng {ListingId}",
            command.InitiatorUserId,
            conversation.Id,
            command.ListingId);

        return Result.Success(MessagingMapper.ToConversationDto(
            conversation,
            command.InitiatorUserId,
            listing,
            initiator,
            recipient,
            lastMessage,
            unreadCount));
    }
}
