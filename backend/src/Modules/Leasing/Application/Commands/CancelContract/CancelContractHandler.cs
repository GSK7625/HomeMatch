using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.CancelContract;

public sealed class CancelContractHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly ILogger<CancelContractHandler> _logger;

    public CancelContractHandler(
        IContractRepository contractRepository,
        ILogger<CancelContractHandler> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        CancelContractCommand command,
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

        var result = contract.Cancel(command.Reason);
        if (result.IsFailure)
        {
            return Result.Failure<ContractDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hợp đồng [ID: {ContractId}] đã bị hủy bởi chủ nhà [Landlord: {LandlordId}]. Lý do: {Reason}",
            contract.Id, command.LandlordId, command.Reason);

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
