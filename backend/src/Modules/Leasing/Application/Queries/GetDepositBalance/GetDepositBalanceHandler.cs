using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetDepositBalance;

public sealed class GetDepositBalanceHandler
{
    private readonly IContractRepository _contractRepository;

    public GetDepositBalanceHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<DepositBalanceDto>> HandleAsync(
        GetDepositBalanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetDetailsByIdAsync(query.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<DepositBalanceDto>(LeasingErrors.NotFound);
        }

        if (query.RequesterId > 0 &&
            contract.LandlordId != query.RequesterId &&
            contract.TenantId != query.RequesterId)
        {
            return Result.Failure<DepositBalanceDto>(LeasingErrors.Forbidden);
        }

        var dto = new DepositBalanceDto(
            ContractId: contract.Id,
            AgreedDepositAmount: contract.DepositAmount,
            TotalReceived: contract.GetTotalReceived(),
            TotalRefunded: contract.GetTotalRefunded(),
            TotalDeducted: contract.GetTotalDeducted(),
            RemainingBalance: contract.GetCurrentDepositBalance(),
            DepositStatus: contract.DepositStatus);

        return Result.Success(dto);
    }
}
