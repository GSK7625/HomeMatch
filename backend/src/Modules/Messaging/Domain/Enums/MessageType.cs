namespace HomeMatch.Modules.Messaging.Domain.Enums;

public static class MessageType
{
    public const string Text = "TEXT";
    public const string Image = "IMAGE";
    public const string System = "SYSTEM";
    public const string AiSuggestion = "AI_SUGGESTION";

    public static readonly string[] All = [Text, Image, System, AiSuggestion];
}
