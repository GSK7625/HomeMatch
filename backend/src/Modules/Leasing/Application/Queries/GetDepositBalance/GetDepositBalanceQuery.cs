namespace HomeMatch.Modules.Leasing.Application.Queries.GetDepositBalance;

public sealed record GetDepositBalanceQuery(
    long ContractId,
    long RequesterId);
