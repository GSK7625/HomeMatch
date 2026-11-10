namespace HomeMatch.Modules.Leasing.Application.Commands.CancelContract;

public sealed record CancelContractCommand(
    long ContractId,
    long LandlordId,
    string Reason);
