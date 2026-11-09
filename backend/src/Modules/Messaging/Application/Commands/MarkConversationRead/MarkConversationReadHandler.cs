using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Messaging.Application.Commands.MarkConversationRead;

public sealed class MarkConversationReadHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatRealtimeNotifier _realtimeNotifier;

    public MarkConversationReadHandler(
        IConversationRepository conversationRepository,
        IChatRealtimeNotifier realtimeNotifier)
    {
        _conversationRepository = conversationRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<Result<int>> HandleAsync(
        MarkConversationReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(command.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure<int>(MessagingErrors.ConversationNotFound);
        }

        if (conversation.InitiatorId != command.ReaderUserId
            && conversation.RecipientId != command.ReaderUserId)
        {
            return Result.Failure<int>(MessagingErrors.Forbidden);
        }

        var markedCount = await _conversationRepository.MarkMessagesAsReadAsync(
            conversation.Id,
            command.ReaderUserId,
            cancellationToken);

        if (markedCount > 0)
        {
            var partnerUserId = conversation.InitiatorId == command.ReaderUserId
                ? conversation.RecipientId
                : conversation.InitiatorId;

            await _realtimeNotifier.NotifyMessagesReadAsync(
                conversation.Id,
                command.ReaderUserId,
                partnerUserId,
                markedCount,
                cancellationToken);
        }

        return Result.Success(markedCount);
    }
}
