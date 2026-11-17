namespace HomeMatch.Modules.Leasing.Application.Commands.RecordDepositTransaction;

public sealed record RecordDepositTransactionCommand(
    long ContractId,
    long LandlordId,
    string TransactionType,
    long Amount,
    string? Reason = null);
