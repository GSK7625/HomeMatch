using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.ConfirmHandover;

public sealed class ConfirmHandoverHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly ILogger<ConfirmHandoverHandler> _logger;

    public ConfirmHandoverHandler(
        IContractRepository contractRepository,
        ILogger<ConfirmHandoverHandler> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        ConfirmHandoverCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.NotFound);
        }

        if (contract.LandlordId != command.LandlordId)
        {
            return Result.Failure<ContractDto>(LeasingErrors.OnlyLandlordCanConfirmHandover);
        }

        var now = DateTimeOffset.UtcNow;
        var actualOccupancyEndDate = command.ActualOccupancyEndDate ?? DateOnly.FromDateTime(now.Date);

        var result = contract.ConfirmHandover(command.LandlordId, now, actualOccupancyEndDate);
        if (result.IsFailure)
        {
            return Result.Failure<ContractDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Chủ nhà [ID: {LandlordId}] đã xác nhận bàn giao phòng cho hợp đồng [ID: {ContractId}]. Trạng thái: COMPLETED",
            command.LandlordId, contract.Id);

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
