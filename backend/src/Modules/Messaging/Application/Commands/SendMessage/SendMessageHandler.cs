using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Messaging.Application.Commands.SendMessage;

public sealed class SendMessageHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IIdentityUserReader _userReader;
    private readonly IChatRealtimeNotifier _realtimeNotifier;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<SendMessageHandler> _logger;

    public SendMessageHandler(
        IConversationRepository conversationRepository,
        IIdentityUserReader userReader,
        IChatRealtimeNotifier realtimeNotifier,
        INotificationPublisher notificationPublisher,
        ILogger<SendMessageHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _userReader = userReader;
        _realtimeNotifier = realtimeNotifier;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<Result<MessageDto>> HandleAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Content))
        {
            return Result.Failure<MessageDto>(MessagingErrors.EmptyMessageContent);
        }

        var normalizedType = string.IsNullOrWhiteSpace(command.MessageType)
            ? MessageType.Text
            : command.MessageType.Trim().ToUpperInvariant();

        if (normalizedType != MessageType.Text && normalizedType != MessageType.Image)
        {
            return Result.Failure<MessageDto>(MessagingErrors.InvalidMessageType);
        }

        var conversation = await _conversationRepository.GetByIdAsync(command.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure<MessageDto>(MessagingErrors.ConversationNotFound);
        }

        if (conversation.InitiatorId != command.SenderUserId
            && conversation.RecipientId != command.SenderUserId)
        {
            return Result.Failure<MessageDto>(MessagingErrors.Forbidden);
        }

        var now = DateTimeOffset.UtcNow;
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = command.SenderUserId,
            MessageType = normalizedType,
            Content = command.Content.Trim(),
            IsRead = false,
            CreatedAt = now
        };

        conversation.LastMessageAt = now;

        await _conversationRepository.AddMessageAsync(message, cancellationToken);
        await _conversationRepository.DeleteDraftAsync(conversation.Id, command.SenderUserId, cancellationToken);
        await _conversationRepository.SaveChangesAsync(cancellationToken);

        var sender = await _userReader.GetUserByIdAsync(command.SenderUserId, cancellationToken);
        var messageDto = MessagingMapper.ToMessageDto(message, sender);

        await _realtimeNotifier.NotifyMessageSentAsync(
            conversation.InitiatorId,
            conversation.RecipientId,
            messageDto,
            cancellationToken);

        var receiverUserId = conversation.InitiatorId == command.SenderUserId
            ? conversation.RecipientId
            : conversation.InitiatorId;

        await _notificationPublisher.PublishAsync(
            userId: receiverUserId,
            title: $"Tin nhắn mới từ {sender?.FullName ?? "Người dùng"}",
            content: message.Content.Length > 120 ? message.Content[..120] + "..." : message.Content,
            type: "NEW_MESSAGE",
            referenceType: "CONVERSATION",
            referenceId: conversation.Id,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Đã gửi tin nhắn #{MessageId} trong hội thoại #{ConversationId} từ UserId {SenderId}",
            message.Id,
            conversation.Id,
            command.SenderUserId);

        return Result.Success(messageDto);
    }
}
