using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface IGeocodingService
{
    Task<Result<IReadOnlyList<GeocodingPlace>>> SearchAsync(string address, CancellationToken cancellationToken);
    Task<Result<GeocodingPlace>> ReverseAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken);
}
