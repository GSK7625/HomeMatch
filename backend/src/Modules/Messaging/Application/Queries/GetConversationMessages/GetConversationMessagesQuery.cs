namespace HomeMatch.Modules.Messaging.Application.Queries.GetConversationMessages;

public record GetConversationMessagesQuery(
    long ConversationId,
    long ActorUserId,
    int Page = 1,
    int PageSize = 50);
