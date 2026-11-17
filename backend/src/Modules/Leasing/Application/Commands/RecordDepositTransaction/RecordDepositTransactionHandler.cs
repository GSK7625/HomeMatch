using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.RecordDepositTransaction;

public sealed class RecordDepositTransactionHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly ILogger<RecordDepositTransactionHandler> _logger;

    public RecordDepositTransactionHandler(
        IContractRepository contractRepository,
        ILogger<RecordDepositTransactionHandler> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public async Task<Result<DepositTransactionDto>> HandleAsync(
        RecordDepositTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetForUpdateAsync(command.ContractId, cancellationToken)
            ?? await _contractRepository.GetDetailsByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<DepositTransactionDto>(LeasingErrors.NotFound);
        }

        if (contract.LandlordId != command.LandlordId)
        {
            return Result.Failure<DepositTransactionDto>(LeasingErrors.Forbidden);
        }

        var result = contract.AddDepositTransaction(
            command.TransactionType,
            command.Amount,
            command.Reason,
            command.LandlordId);

        if (result.IsFailure)
        {
            return Result.Failure<DepositTransactionDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã ghi nhận giao dịch cọc [{Type}, {Amount} VND] cho hợp đồng [ID: {ContractId}] bởi [User: {RecordedBy}]",
            command.TransactionType, command.Amount, contract.Id, command.LandlordId);

        return Result.Success(ContractMapper.ToDto(result.Value));
    }
}
