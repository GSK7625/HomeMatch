using HomeMatch.Modules.Properties.Application;
using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Repositories;

public sealed class PropertyRepository(PropertiesDbContext db) : IPropertyRepository
{
    /// Kiểm tra quyền sở hữu đối với tòa nhà
    public Task<Property?> GetOwnedAsync(long landlordId, long propertyId, CancellationToken cancellationToken) =>
        db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.LandlordId == landlordId, cancellationToken);

    public Task<bool> IsOwnedAsync(long landlordId, long propertyId, CancellationToken cancellationToken) =>
        db.Properties.AsNoTracking().AnyAsync(p => p.Id == propertyId && p.LandlordId == landlordId, cancellationToken);

    public async Task<IReadOnlyList<PropertyResult>> GetMineAsync(long landlordId, CancellationToken cancellationToken) =>
        await db.Properties.AsNoTracking().Where(p => p.LandlordId == landlordId)
            .OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
            .Select(p => new PropertyResult(p.Id, p.Name, p.ProvinceCode, p.WardCode,
                p.AddressDetail, p.PropertyType, p.Latitude, p.Longitude)).ToListAsync(cancellationToken);

    public async Task<string?> GetFullAddressAsync(string provinceCode, string wardCode, string addressDetail,
        CancellationToken cancellationToken)
    {
        var parts = await (
            from ward in db.AdministrativeWards.AsNoTracking()
            join province in db.AdministrativeProvinces.AsNoTracking() on ward.ProvinceCode equals province.Code
            where ward.Code == wardCode.Trim() && province.Code == provinceCode.Trim()
            select new { Ward = ward.FullName, ward.DistrictName, Province = province.FullName })
            .SingleOrDefaultAsync(cancellationToken);
        return parts is null ? null : string.Join(", ", new[]
        {
            addressDetail.Trim(), parts.Ward, parts.DistrictName, parts.Province, "Việt Nam"
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public async Task<IReadOnlyList<long>> GetRoomIdsAsync(long propertyId, CancellationToken cancellationToken) =>
        await db.Rooms.Where(r => r.PropertyId == propertyId).Select(r => r.Id).ToListAsync(cancellationToken);

    public void Add(Property property) => db.Properties.Add(property);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
