using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Messaging.Application.Commands.SaveMessageDraft;

public sealed class SaveMessageDraftHandler
{
    private readonly IConversationRepository _conversationRepository;

    public SaveMessageDraftHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<Result<MessageDraftDto>> HandleAsync(
        SaveMessageDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Content))
        {
            return Result.Failure<MessageDraftDto>(MessagingErrors.EmptyMessageContent);
        }

        var conversation = await _conversationRepository.GetByIdAsync(command.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure<MessageDraftDto>(MessagingErrors.ConversationNotFound);
        }

        if (conversation.InitiatorId != command.AuthorUserId
            && conversation.RecipientId != command.AuthorUserId)
        {
            return Result.Failure<MessageDraftDto>(MessagingErrors.Forbidden);
        }

        var source = string.Equals(command.Source, MessageDraftSource.AiAssistant, StringComparison.OrdinalIgnoreCase)
            ? MessageDraftSource.AiAssistant
            : MessageDraftSource.Manual;

        var draft = await _conversationRepository.UpsertDraftAsync(
            conversation.Id,
            command.AuthorUserId,
            command.Content.Trim(),
            source,
            cancellationToken);

        await _conversationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new MessageDraftDto(
            Id: draft.Id,
            ConversationId: draft.ConversationId,
            AuthorId: draft.AuthorId,
            Source: draft.Source,
            Content: draft.Content,
            UpdatedAt: draft.UpdatedAt));
    }
}
