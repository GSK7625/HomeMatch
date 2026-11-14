namespace HomeMatch.Modules.Leasing.Application.Commands.ProcessContractLifecycle;

public sealed record ProcessContractLifecycleCommand(
    DateOnly? Today = null,
    DateTimeOffset? Now = null);

public sealed record ProcessContractLifecycleResult(
    int ActivatedCount,
    int ExpiredPendingHandoverCount,
    int HandoverOverdueCount,
    int TotalProcessed,
    int ErrorCount);
