using HomeMatch.Modules.Messaging.Domain.Enums;

namespace HomeMatch.Modules.Messaging.Application.Commands.SaveMessageDraft;

public record SaveMessageDraftCommand(
    long ConversationId,
    long AuthorUserId,
    string Content,
    string Source = MessageDraftSource.Manual);
