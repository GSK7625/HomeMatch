namespace HomeMatch.Modules.Leasing.Application.Commands.ActivateContract;

public sealed record ActivateContractCommand(
    long ContractId,
    DateOnly? Today = null);
