namespace HomeMatch.Modules.Leasing.Application.Commands.ExpirePendingSignatures;

public sealed record ExpirePendingSignaturesCommand(DateTimeOffset? AsOf = null);

public sealed record ExpirePendingSignaturesResult(
    int TotalProcessed,
    int ExpiredCount,
    int SkippedCount,
    int ErrorCount);
