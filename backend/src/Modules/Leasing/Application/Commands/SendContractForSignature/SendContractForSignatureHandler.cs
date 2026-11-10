using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.SendContractForSignature;

public sealed class SendContractForSignatureHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly ILogger<SendContractForSignatureHandler> _logger;

    public SendContractForSignatureHandler(
        IContractRepository contractRepository,
        ILogger<SendContractForSignatureHandler> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        SendContractForSignatureCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.NotFound);
        }

        if (contract.LandlordId != command.LandlordId)
        {
            return Result.Failure<ContractDto>(LeasingErrors.Forbidden);
        }

        // Kiểm tra lịch giữ phòng trước khi chuyển sang PENDING_SIGNATURE
        var hasOverlap = await _contractRepository.HasOverlappingOccupancyAsync(
            contract.RoomId,
            contract.StartDate,
            contract.EndDate,
            excludeContractId: contract.Id,
            cancellationToken);

        if (hasOverlap)
        {
            return Result.Failure<ContractDto>(LeasingErrors.ScheduleConflict);
        }

        var result = contract.SendForSignature(command.SignatureExpiresAt, command.HandoverDueAt);
        if (result.IsFailure)
        {
            return Result.Failure<ContractDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hợp đồng [ID: {ContractId}] đã được gửi ký. Hạn chót: {ExpiresAt}",
            contract.Id, command.SignatureExpiresAt);

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
