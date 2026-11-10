using HomeMatch.Modules.Leasing.Domain.Entities;

namespace HomeMatch.Modules.Leasing.Application.DTOs;

public static class ContractMapper
{
    public static ContractDto ToDto(Contract contract) => new(
        contract.Id,
        contract.ContractCode,
        contract.RoomId,
        contract.TenantId,
        contract.LandlordId,
        contract.StartDate,
        contract.EndDate,
        contract.MonthlyRent,
        contract.DepositAmount,
        contract.DepositStatus,
        contract.BillingDueDay,
        contract.ContractFileUrl,
        contract.Status,
        contract.SignatureExpiresAt,
        contract.LandlordSignedAt,
        contract.TenantSignedAt,
        contract.SignedDocumentHash,
        contract.HandoverDueAt,
        contract.HandoverConfirmedAt,
        contract.HandoverConfirmedBy,
        contract.ActualOccupancyEndDate,
        contract.CreatedAt,
        contract.UpdatedAt);

    public static DepositTransactionDto ToDto(DepositTransaction tx) => new(
        tx.Id,
        tx.ContractId,
        tx.TransactionType,
        tx.Amount,
        tx.Reason,
        tx.RecordedBy,
        tx.CreatedAt);

    public static ContractDetailDto ToDetailDto(Contract contract) => new(
        contract.Id,
        contract.ContractCode,
        contract.RoomId,
        contract.TenantId,
        contract.LandlordId,
        contract.StartDate,
        contract.EndDate,
        contract.MonthlyRent,
        contract.DepositAmount,
        contract.DepositStatus,
        contract.BillingDueDay,
        contract.ContractFileUrl,
        contract.Status,
        contract.SignatureExpiresAt,
        contract.LandlordSignedAt,
        contract.TenantSignedAt,
        contract.SignedDocumentHash,
        contract.HandoverDueAt,
        contract.HandoverConfirmedAt,
        contract.HandoverConfirmedBy,
        contract.ActualOccupancyEndDate,
        contract.CreatedAt,
        contract.UpdatedAt,
        contract.GetTotalReceived(),
        contract.GetTotalRefunded(),
        contract.GetTotalDeducted(),
        contract.GetCurrentDepositBalance(),
        contract.DepositTransactions.Select(ToDto).ToList());
}
