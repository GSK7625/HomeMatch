using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetExpiringContracts;

public sealed class GetExpiringContractsHandler
{
    private readonly IContractRepository _contractRepository;

    public GetExpiringContractsHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<IReadOnlyList<ContractDto>>> HandleAsync(
        GetExpiringContractsQuery query,
        CancellationToken cancellationToken = default)
    {
        var days = query.Days > 0 ? query.Days : 30;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var toDate = today.AddDays(days);

        var contracts = await _contractRepository.GetExpiringContractsForLandlordAsync(
            query.LandlordId,
            today,
            toDate,
            cancellationToken);

        var dtos = contracts.Select(ContractMapper.ToDto).ToList();
        return Result.Success<IReadOnlyList<ContractDto>>(dtos);
    }
}
