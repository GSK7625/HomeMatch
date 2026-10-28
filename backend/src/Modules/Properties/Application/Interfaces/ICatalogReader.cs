using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface ICatalogReader
{
    Task<IReadOnlyList<ProvinceResponse>> GetProvincesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WardResponse>> GetWardsAsync(string provinceCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<AmenityResponse>> GetAmenitiesAsync(CancellationToken cancellationToken);
}
