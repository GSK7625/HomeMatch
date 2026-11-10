namespace HomeMatch.Modules.Leasing.Application.Commands.MarkHandoverOverdue;

public sealed record MarkHandoverOverdueCommand(
    long ContractId,
    DateOnly? Today = null);
