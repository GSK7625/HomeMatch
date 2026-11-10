namespace HomeMatch.Modules.Leasing.Application.DTOs;

public sealed record DepositTransactionDto(
    long Id,
    long ContractId,
    string TransactionType,
    long Amount,
    string? Reason,
    long RecordedBy,
    DateTimeOffset CreatedAt);
