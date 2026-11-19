namespace HomeMatch.Modules.Billing.Application.Commands.PayContractDeposit;

public sealed record PayContractDepositCommand(
    long ContractId,
    long UserId,
    bool IsAdmin,
    string ClientIpAddress);
