namespace HomeMatch.Modules.Messaging.Application.Commands.MarkConversationRead;

public record MarkConversationReadCommand(
    long ConversationId,
    long ReaderUserId);
