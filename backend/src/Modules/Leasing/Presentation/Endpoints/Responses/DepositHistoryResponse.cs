namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public sealed record DepositHistoryResponse(
    long ContractId,
    long AgreedDepositAmount,
    long TotalReceived,
    long TotalRefunded,
    long TotalDeducted,
    long CurrentBalance,
    IReadOnlyList<DepositTransactionResponse> Transactions);
