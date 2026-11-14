namespace HomeMatch.Modules.Leasing.Application.DTOs;

public sealed record DepositBalanceDto(
    long ContractId,
    long AgreedDepositAmount,
    long TotalReceived,
    long TotalRefunded,
    long TotalDeducted,
    long RemainingBalance,
    string DepositStatus);
