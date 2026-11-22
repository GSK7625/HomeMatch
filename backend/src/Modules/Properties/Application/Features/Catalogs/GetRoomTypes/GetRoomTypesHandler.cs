using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Catalogs.GetRoomTypes;

public sealed record GetRoomTypesQuery;

public sealed class GetRoomTypesHandler(ICatalogReader catalogs)
{
    public async Task<Result<IReadOnlyList<RoomTypeResult>>> HandleAsync(
        GetRoomTypesQuery query,
        CancellationToken cancellationToken = default) =>
        Result.Success(await catalogs.GetRoomTypesAsync(cancellationToken));
}
