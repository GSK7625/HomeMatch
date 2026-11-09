using HomeMatch.Modules.Messaging.Domain.Enums;

namespace HomeMatch.Modules.Messaging.Domain.Entities;

public class MessageDraft
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long AuthorId { get; set; }
    public string Source { get; set; } = MessageDraftSource.Manual;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Conversation? Conversation { get; set; }
}
