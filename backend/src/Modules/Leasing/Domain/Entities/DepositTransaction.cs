using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Domain.Entities;

/// <summary>
/// Thực thể Giao dịch tiền cọc độc lập (Nhận, Hoàn trả, Khấu trừ)
/// </summary>
public class DepositTransaction
{
    public long Id { get; set; }
    public long ContractId { get; set; }
    public string TransactionType { get; set; } = DepositTransactionType.Received;
    public long Amount { get; set; }
    public string? Reason { get; set; }
    public long RecordedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Contract Contract { get; set; } = null!;

    public static Result<DepositTransaction> Create(
        long contractId,
        string transactionType,
        long amount,
        string? reason,
        long recordedBy)
    {
        if (contractId <= 0)
        {
            return Result.Failure<DepositTransaction>(new Error("Leasing.InvalidContractId", "Mã hợp đồng không hợp lệ."));
        }

        if (amount <= 0)
        {
            return Result.Failure<DepositTransaction>(LeasingErrors.InvalidDepositTransactionAmount);
        }

        if (transactionType != DepositTransactionType.Received &&
            transactionType != DepositTransactionType.Refunded &&
            transactionType != DepositTransactionType.Deducted)
        {
            return Result.Failure<DepositTransaction>(LeasingErrors.InvalidDepositTransactionType);
        }

        if (transactionType == DepositTransactionType.Deducted && string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<DepositTransaction>(LeasingErrors.DeductionReasonRequired);
        }

        var tx = new DepositTransaction
        {
            ContractId = contractId,
            TransactionType = transactionType,
            Amount = amount,
            Reason = reason?.Trim(),
            RecordedBy = recordedBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(tx);
    }
}
