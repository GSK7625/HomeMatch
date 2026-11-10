namespace HomeMatch.Modules.Leasing.Application.Commands.MarkContractExpiredPendingHandover;

public sealed record MarkContractExpiredCommand(
    long ContractId,
    DateOnly? Today = null);
