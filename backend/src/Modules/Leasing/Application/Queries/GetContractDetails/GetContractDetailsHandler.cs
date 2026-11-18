using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractDetails;

public sealed class GetContractDetailsHandler
{
    private readonly IContractRepository _contractRepository;

    public GetContractDetailsHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<ContractDetailDto>> HandleAsync(
        GetContractDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetDetailsByIdAsync(query.Id, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDetailDto>(LeasingErrors.NotFound);
        }

        if (query.RequesterId > 0 &&
            contract.LandlordId != query.RequesterId &&
            contract.TenantId != query.RequesterId)
        {
            return Result.Failure<ContractDetailDto>(LeasingErrors.Forbidden);
        }

        return Result.Success(ContractMapper.ToDetailDto(contract));
    }
}
