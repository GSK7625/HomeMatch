using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Catalogs.GetProvinces;

public sealed class GetProvincesHandler
{
    private readonly ICatalogReader _catalogs;

    public GetProvincesHandler(ICatalogReader catalogs)
    {
        _catalogs = catalogs;
    }

    public async Task<Result<IReadOnlyList<ProvinceResult>>> HandleAsync(GetProvincesQuery command, CancellationToken cancellationToken = default)
    {
        return Result.Success(await _catalogs.GetProvincesAsync(cancellationToken));
    }
}
