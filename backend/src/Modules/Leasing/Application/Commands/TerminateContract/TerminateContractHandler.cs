using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.TerminateContract;

public sealed class TerminateContractHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly ILogger<TerminateContractHandler> _logger;

    public TerminateContractHandler(
        IContractRepository contractRepository,
        ILogger<TerminateContractHandler> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        TerminateContractCommand command,
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

        var result = contract.TerminateEarly(command.Reason, DateTimeOffset.UtcNow);
        if (result.IsFailure)
        {
            return Result.Failure<ContractDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hợp đồng [ID: {ContractId}] đã bị chấm dứt trước hạn bởi chủ nhà [ID: {LandlordId}]. Lý do: {Reason}",
            contract.Id, command.LandlordId, command.Reason);

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
