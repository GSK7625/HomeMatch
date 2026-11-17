using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Properties.Application.Features.Properties.UpdateProperty;

public sealed class UpdatePropertyHandler
{
    private readonly IPropertyRepository _properties;
    private readonly IRoomListingEditReader _listingEditReader;

    public UpdatePropertyHandler(IPropertyRepository properties, IRoomListingEditReader listingEditReader)
    {
        _properties = properties;
        _listingEditReader = listingEditReader;
    }

    public async Task<Result<PropertyResult>> HandleAsync(UpdatePropertyCommand command, CancellationToken cancellationToken = default)
    {
        var property = await _properties.GetOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken);
        if (property is null) return Result.Failure<PropertyResult>(PropertiesErrors.PropertyNotFound);
        var roomIds = await _properties.GetRoomIdsAsync(command.PropertyId, cancellationToken);
        // shortcut: this check does not serialize property edits with submit/approve; use a shared lock if atomic transitions are required.
        if (await _listingEditReader.IsAnyRoomEditBlockedAsync(roomIds, cancellationToken))
            return Result.Failure<PropertyResult>(PropertiesErrors.PropertyEditLocked);
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

        error = PropertyValidation.ValidateCoordinates(command.Latitude, command.Longitude, required: addressChanged);
        if (error is not null) return Result.Failure<PropertyResult>(error);

        if (command.Latitude.HasValue && command.Longitude.HasValue)
        {
            property.Latitude = decimal.Round(command.Latitude.Value, 6);
            property.Longitude = decimal.Round(command.Longitude.Value, 6);
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
