using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface ICatalogReader
{
    Task<IReadOnlyList<ProvinceResult>> GetProvincesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WardResult>> GetWardsAsync(string provinceCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<AmenityResult>> GetAmenitiesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomTypeResult>> GetRoomTypesAsync(CancellationToken cancellationToken);
    Task<bool> RoomTypeExistsAsync(string code, bool activeOnly, CancellationToken cancellationToken);
}
