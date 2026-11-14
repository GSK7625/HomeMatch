namespace HomeMatch.Modules.Leasing.Application.Commands.SendContractExpirationReminders;

public sealed record SendContractExpirationRemindersCommand(
    DateOnly? Today = null,
    DateTimeOffset? Now = null,
    int DaysThreshold = 30);

public sealed record SendContractExpirationRemindersResult(
    int TotalCandidates,
    int RemindersSent,
    int SkippedCount,
    int ErrorCount);
