namespace HomeMatch.Modules.Messaging.Application.Queries.GetConversations;

public record GetConversationsQuery(
    long UserId,
    int Page = 1,
    int PageSize = 20);
