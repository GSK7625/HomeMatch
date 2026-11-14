namespace HomeMatch.Modules.Leasing.Application.Commands.TerminateContract;

public sealed record TerminateContractCommand(
    long ContractId,
    long LandlordId,
    string Reason);
