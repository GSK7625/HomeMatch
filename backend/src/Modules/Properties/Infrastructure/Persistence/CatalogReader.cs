using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class CatalogReader(PropertiesDbContext db) : ICatalogReader
{
    public async Task<IReadOnlyList<ProvinceResponse>> GetProvincesAsync(
        CancellationToken cancellationToken) =>
        await db.AdministrativeProvinces.AsNoTracking().OrderBy(province => province.Name)
            .Select(province => new ProvinceResponse(province.Code, province.Name, province.FullName))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WardResponse>> GetWardsAsync(
        string provinceCode, CancellationToken cancellationToken) =>
        await db.AdministrativeWards.AsNoTracking()
            .Where(ward => ward.ProvinceCode == provinceCode)
            .OrderBy(ward => ward.Name)
            .Select(ward => new WardResponse(ward.Code, ward.Name, ward.FullName, ward.ProvinceCode))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AmenityResponse>> GetAmenitiesAsync(
        CancellationToken cancellationToken) =>
        await db.Amenities.AsNoTracking()
            .OrderBy(amenity => amenity.Category).ThenBy(amenity => amenity.Name)
            .Select(amenity => new AmenityResponse(amenity.Id, amenity.Code, amenity.Name,
                amenity.Category, amenity.IconUrl))
            .ToListAsync(cancellationToken);
}
