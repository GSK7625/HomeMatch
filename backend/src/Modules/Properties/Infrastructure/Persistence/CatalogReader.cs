using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class CatalogReader(PropertiesDbContext db) : ICatalogReader
{
    public async Task<IReadOnlyList<ProvinceResult>> GetProvincesAsync(
        CancellationToken cancellationToken) =>
        await db.AdministrativeProvinces.AsNoTracking().OrderBy(province => province.Name)
            .Select(province => new ProvinceResult(province.Code, province.Name, province.FullName))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WardResult>> GetWardsAsync(
        string provinceCode, CancellationToken cancellationToken) =>
        await db.AdministrativeWards.AsNoTracking()
            .Where(ward => ward.ProvinceCode == provinceCode)
            .OrderBy(ward => ward.Name)
            .Select(ward => new WardResult(ward.Code, ward.Name, ward.FullName, ward.ProvinceCode))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AmenityResult>> GetAmenitiesAsync(
        CancellationToken cancellationToken) =>
        await db.Amenities.AsNoTracking()
            .OrderBy(amenity => amenity.Id)
            .Select(amenity => new AmenityResult(amenity.Id, amenity.Code, amenity.Name))
            .ToListAsync(cancellationToken);
}
