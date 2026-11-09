using HomeMatch.Modules.Messaging.Domain.Enums;

namespace HomeMatch.Modules.Messaging.Domain.Entities;

public class Message
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string MessageType { get; set; } = Enums.MessageType.Text;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Conversation? Conversation { get; set; }
}
