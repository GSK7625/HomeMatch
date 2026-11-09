using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Catalogs.GetWards;

public sealed class GetWardsHandler
{
    private readonly ICatalogReader _catalogs;

    public GetWardsHandler(ICatalogReader catalogs)
    {
        _catalogs = catalogs;
    }

    public async Task<Result<IReadOnlyList<WardResult>>> HandleAsync(GetWardsQuery command, CancellationToken cancellationToken = default)
    {
        return Result.Success(await _catalogs.GetWardsAsync(command.ProvinceCode, cancellationToken));
    }
}
