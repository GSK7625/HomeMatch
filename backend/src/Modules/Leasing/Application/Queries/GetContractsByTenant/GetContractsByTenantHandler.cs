using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractsByTenant;

public sealed class GetContractsByTenantHandler
{
    private readonly IContractRepository _contractRepository;

    public GetContractsByTenantHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<PagedContractsDto>> HandleAsync(
        GetContractsByTenantQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _contractRepository.GetPagedByTenantAsync(
            query.TenantId,
            query.Status,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(ContractMapper.ToDto).ToList();
        return Result.Success(new PagedContractsDto(dtos, page, pageSize, totalCount));
    }
}
