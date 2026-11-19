using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractById;

public sealed class GetContractByIdHandler
{
    private readonly IContractRepository _contractRepository;

    public GetContractByIdHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        GetContractByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(query.Id, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.NotFound);
        }

        if (query.RequesterId > 0 &&
            contract.LandlordId != query.RequesterId &&
            contract.TenantId != query.RequesterId)
        {
            return Result.Failure<ContractDto>(LeasingErrors.Forbidden);
        }

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
