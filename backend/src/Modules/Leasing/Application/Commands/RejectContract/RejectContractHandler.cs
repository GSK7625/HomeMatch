using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.RejectContract;

public sealed class RejectContractHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly ILogger<RejectContractHandler> _logger;

    public RejectContractHandler(
        IContractRepository contractRepository,
        ILogger<RejectContractHandler> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        RejectContractCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.NotFound);
        }

        if (command.ActorId != contract.LandlordId && command.ActorId != contract.TenantId)
        {
            return Result.Failure<ContractDto>(LeasingErrors.Forbidden);
        }

        var result = contract.Reject(command.Reason);
        if (result.IsFailure)
        {
            return Result.Failure<ContractDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hợp đồng [ID: {ContractId}] đã bị từ chối bởi [User: {ActorId}]. Lý do: {Reason}",
            contract.Id, command.ActorId, command.Reason);

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
