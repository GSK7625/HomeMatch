namespace HomeMatch.Modules.Leasing.Application.Commands.RejectContract;

public sealed record RejectContractCommand(
    long ContractId,
    long ActorId,
    string Reason);
