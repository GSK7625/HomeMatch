namespace HomeMatch.Modules.Messaging.Domain.Enums;

public static class MessageDraftSource
{
    public const string Manual = "MANUAL";
    public const string AiAssistant = "AI_ASSISTANT";

    public static readonly string[] All = [Manual, AiAssistant];
}
