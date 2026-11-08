using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Catalogs.GetAmenities;

public sealed class GetAmenitiesHandler
{
    private readonly ICatalogReader _catalogs;

    public GetAmenitiesHandler(ICatalogReader catalogs)
    {
        _catalogs = catalogs;
    }

    public async Task<Result<IReadOnlyList<AmenityResult>>> HandleAsync(GetAmenitiesQuery command, CancellationToken cancellationToken = default)
    {
        return Result.Success(await _catalogs.GetAmenitiesAsync(cancellationToken));
    }
}
