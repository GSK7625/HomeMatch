using HomeMatch.Modules.Messaging.Domain.Enums;

namespace HomeMatch.Modules.Messaging.Domain.Entities;

public class Conversation
{
    public long Id { get; set; }
    public long InitiatorId { get; set; }
    public long RecipientId { get; set; }
    public long? ListingId { get; set; }
    public long? RoommatePostId { get; set; }
    public long? AiSessionId { get; set; }
    public string ConversationType { get; set; } = Enums.ConversationType.Listing;
    public long UserMinId { get; private set; }
    public long UserMaxId { get; private set; }
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<MessageDraft> Drafts { get; set; } = new List<MessageDraft>();
}
