using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Properties.Application.Features.Properties.UpdateProperty;

public sealed class UpdatePropertyHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IAddressGeocoder _geocoder;
    private readonly IListingVisibilityWriter _listingVisibility;

    public UpdatePropertyHandler(IPropertyRepository properties, IAddressGeocoder geocoder, IListingVisibilityWriter listingVisibility)
    {
        _properties = properties;
        _geocoder = geocoder;
        _listingVisibility = listingVisibility;
    }

    public async Task<Result<PropertyResult>> HandleAsync(UpdatePropertyCommand command, CancellationToken cancellationToken = default)
    {
        var property = await _properties.GetOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken);
        if (property is null) return Result.Failure<PropertyResult>(PropertiesErrors.PropertyNotFound);
        var name = command.Name ?? property.Name;
        var province = command.ProvinceCode ?? property.ProvinceCode;
        var ward = command.WardCode ?? property.WardCode;
        var address = command.AddressDetail ?? property.AddressDetail;
        var type = command.PropertyType ?? property.PropertyType;
        var error = PropertyValidation.Validate(name, province, ward, address, type);
        if (error is not null) return Result.Failure<PropertyResult>(error);
        var fullAddress = await _properties.GetFullAddressAsync(province, ward, address, cancellationToken);
        if (fullAddress is null) return Result.Failure<PropertyResult>(PropertiesErrors.InvalidAddress);
        var addressChanged = province.Trim() != property.ProvinceCode || ward.Trim() != property.WardCode
            || address.Trim() != property.AddressDetail;
        if (addressChanged)
        {
            var location = await _geocoder.FindAsync(fullAddress, cancellationToken);
            // An unresolved new address must never retain the old address's coordinates.
            property.Latitude = location?.Latitude;
            property.Longitude = location?.Longitude;
            var roomIds = await _properties.GetRoomIdsAsync(command.PropertyId, cancellationToken);
            foreach (var roomId in roomIds)
                await _listingVisibility.HidePublishedForRoomAsync(roomId, cancellationToken);
        }
        property.Name = name.Trim();
        property.ProvinceCode = province.Trim();
        property.WardCode = ward.Trim();
        property.AddressDetail = address.Trim();
        property.PropertyType = type;
        property.UpdatedAt = DateTimeOffset.UtcNow;
        await _properties.SaveChangesAsync(cancellationToken);
        return Result.Success(PropertyMapping.ToResult(property));
    }
}
