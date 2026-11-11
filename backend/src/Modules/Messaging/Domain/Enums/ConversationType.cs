namespace HomeMatch.Modules.Messaging.Domain.Enums;

public static class ConversationType
{
    public const string Listing = "LISTING";
    public const string Roommate = "ROOMMATE";
    public const string AiEscalated = "AI_ESCALATED";

    public static readonly string[] All = [Listing, Roommate, AiEscalated];
}
