namespace HomeMatch.Modules.Leasing.Application.Queries.GetDepositTransactions;

public sealed record GetDepositTransactionsQuery(
    long ContractId,
    long RequesterId);
