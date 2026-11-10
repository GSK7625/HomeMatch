using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.MarkHandoverOverdue;

public sealed class MarkHandoverOverdueHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly ILogger<MarkHandoverOverdueHandler> _logger;

    public MarkHandoverOverdueHandler(
        IContractRepository contractRepository,
        ILogger<MarkHandoverOverdueHandler> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        MarkHandoverOverdueCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.NotFound);
        }

        var today = command.Today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = contract.MarkHandoverOverdue(today);
        if (result.IsFailure)
        {
            return Result.Failure<ContractDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hợp đồng [ID: {ContractId}] đã chuyển sang HANDOVER_OVERDUE", contract.Id);

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
