namespace HomeMatch.Modules.Leasing.Application.DTOs;

public sealed record DepositHistoryDto(
    long ContractId,
    long AgreedDepositAmount,
    long TotalReceived,
    long TotalRefunded,
    long TotalDeducted,
    long CurrentBalance,
    IReadOnlyList<DepositTransactionDto> Transactions);
