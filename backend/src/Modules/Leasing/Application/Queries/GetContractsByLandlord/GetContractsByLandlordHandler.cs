using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractsByLandlord;

public sealed class GetContractsByLandlordHandler
{
    private readonly IContractRepository _contractRepository;

    public GetContractsByLandlordHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<PagedContractsDto>> HandleAsync(
        GetContractsByLandlordQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _contractRepository.GetPagedByLandlordAsync(
            query.LandlordId,
            query.Status,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(ContractMapper.ToDto).ToList();
        return Result.Success(new PagedContractsDto(dtos, page, pageSize, totalCount));
    }
}
