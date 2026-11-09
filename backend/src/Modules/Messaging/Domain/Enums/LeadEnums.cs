namespace HomeMatch.Modules.Messaging.Domain.Enums;

public static class LeadStatus
{
    public const string New = "NEW";
    public const string Contacted = "CONTACTED";
    public const string ViewingScheduled = "VIEWING_SCHEDULED";
    public const string ContractSigned = "CONTRACT_SIGNED";
    public const string Lost = "LOST";

    public static readonly string[] All = [New, Contacted, ViewingScheduled, ContractSigned, Lost];
}

public static class LeadSource
{
    public const string Direct = "DIRECT";
    public const string SearchFilter = "SEARCH_FILTER";
    public const string Comparison = "COMPARISON";
    public const string AiChat = "AI_CHAT";

    public static readonly string[] All = [Direct, SearchFilter, Comparison, AiChat];
}
