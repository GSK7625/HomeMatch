namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public sealed record DepositTransactionResponse(
    long Id,
    long ContractId,
    string TransactionType,
    long Amount,
    string? Reason,
    long RecordedBy,
    DateTimeOffset CreatedAt);
