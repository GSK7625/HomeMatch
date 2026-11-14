namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public sealed record DepositBalanceResponse(
    long ContractId,
    long AgreedDepositAmount,
    long TotalReceived,
    long TotalRefunded,
    long TotalDeducted,
    long RemainingBalance,
    string DepositStatus);
