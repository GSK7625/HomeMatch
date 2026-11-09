namespace HomeMatch.Modules.Messaging.Application.DTOs;

public record MessageDto(
    long Id,
    long ConversationId,
    long SenderId,
    string SenderName,
    string MessageType,
    string Content,
    bool IsRead,
    DateTimeOffset CreatedAt);

public record ConversationDto(
    long Id,
    string ConversationType,
    long? ListingId,
    string? ListingTitle,
    decimal? ListingPrice,
    string? ListingPrimaryImageUrl,
    long InitiatorId,
    string InitiatorName,
    long RecipientId,
    string RecipientName,
    long PartnerId,
    string PartnerName,
    string? PartnerPhone,
    string? LastMessageContent,
    long? LastMessageSenderId,
    int UnreadCount,
    DateTimeOffset LastMessageAt,
    DateTimeOffset CreatedAt);

public record MessageDraftDto(
    long Id,
    long ConversationId,
    long AuthorId,
    string Source,
    string Content,
    DateTimeOffset UpdatedAt);

public record PagedConversationsDto(
    IReadOnlyList<ConversationDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record PagedMessagesDto(
    ConversationDto Conversation,
    IReadOnlyList<MessageDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
