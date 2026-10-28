using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Modules.Properties.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Application;

/// <summary>
/// Dịch vụ quản lý Bất động sản (Property).
/// Tuân thủ SRP — chỉ xử lý nghiệp vụ liên quan đến Property, không xử lý Room.
/// Tuân thủ DIP — implement IPropertyService, được inject qua interface.
/// </summary>
public sealed class PropertyService(
    PropertiesDbContext db,
    IListingVisibilityWriter listingVisibility) : IPropertyService
{
    private static readonly HashSet<string> ValidPropertyTypes =
        ["BOARDING_HOUSE", "APARTMENT", "MINI_APARTMENT", "WHOLE_HOUSE"];

    /// <inheritdoc/>
    public async Task<PropertyResponse> CreateAsync(
        long landlordId, CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Name, request.ProvinceCode, request.WardCode,
            request.AddressDetail, request.PropertyType);
        await EnsureAddressExistsAsync(request.ProvinceCode, request.WardCode, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var property = new Property
        {
            LandlordId = landlordId,
            Name = request.Name.Trim(),
            ProvinceCode = request.ProvinceCode.Trim(),
            WardCode = request.WardCode.Trim(),
            AddressDetail = request.AddressDetail.Trim(),
            PropertyType = request.PropertyType,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(property);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PropertyResponse>> GetMineAsync(
        long landlordId, CancellationToken cancellationToken) =>
        await db.Properties.AsNoTracking()
            .Where(p => p.LandlordId == landlordId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PropertyResponse(p.Id, p.Name, p.ProvinceCode,
                p.WardCode, p.AddressDetail, p.PropertyType))
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<PropertyResponse> GetByIdAsync(
        long landlordId, long propertyId, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedPropertyAsync(landlordId, propertyId, cancellationToken));

    /// <inheritdoc/>
    public async Task<PropertyResponse> UpdateAsync(
        long landlordId, long propertyId, UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var property = await GetOwnedPropertyAsync(landlordId, propertyId, cancellationToken);
        var name = request.Name ?? property.Name;
        var province = request.ProvinceCode ?? property.ProvinceCode;
        var ward = request.WardCode ?? property.WardCode;
        var address = request.AddressDetail ?? property.AddressDetail;
        var propertyType = request.PropertyType ?? property.PropertyType;

        Validate(name, province, ward, address, propertyType);
        await EnsureAddressExistsAsync(province, ward, cancellationToken);

        var addressChanged = province != property.ProvinceCode
            || ward != property.WardCode
            || address != property.AddressDetail;

        if (addressChanged)
        {
            var roomIds = await db.Rooms
                .Where(r => r.PropertyId == propertyId)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            foreach (var roomId in roomIds)
                await listingVisibility.HidePublishedForRoomAsync(roomId, cancellationToken);
        }

        property.Name = name.Trim();
        property.ProvinceCode = province.Trim();
        property.WardCode = ward.Trim();
        property.AddressDetail = address.Trim();
        property.PropertyType = propertyType;
        property.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(property);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<Property> GetOwnedPropertyAsync(
        long landlordId, long propertyId, CancellationToken cancellationToken) =>
        await db.Properties.FirstOrDefaultAsync(
            p => p.Id == propertyId && p.LandlordId == landlordId, cancellationToken)
        ?? throw ApiException.NotFound("Property.NotFound", "Không tìm thấy căn thuộc tài khoản này.");

    private async Task EnsureAddressExistsAsync(
        string provinceCode, string wardCode, CancellationToken cancellationToken)
    {
        var valid = await db.AdministrativeWards.AnyAsync(
            w => w.Code == wardCode.Trim() && w.ProvinceCode == provinceCode.Trim(),
            cancellationToken);
        if (!valid)
            throw ApiException.BadRequest("Property.InvalidAddress",
                "Phường/xã không thuộc tỉnh/thành phố đã chọn.");
    }

    private static void Validate(
        string name, string province, string ward, string address, string propertyType)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200
            || string.IsNullOrWhiteSpace(province) || province.Length > 20
            || string.IsNullOrWhiteSpace(ward) || ward.Length > 20
            || string.IsNullOrWhiteSpace(address) || address.Length > 255
            || !ValidPropertyTypes.Contains(propertyType))
            throw ApiException.BadRequest("Property.InvalidInput", "Thông tin căn không hợp lệ.");
    }

    private static PropertyResponse ToResponse(Property property) =>
        new(property.Id, property.Name, property.ProvinceCode,
            property.WardCode, property.AddressDetail, property.PropertyType);
}
