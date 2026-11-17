using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Properties.CreateProperty;

public sealed class CreatePropertyHandler
{
    private readonly IPropertyRepository _properties;

    public CreatePropertyHandler(IPropertyRepository properties)
    {
        _properties = properties;
    }

    public async Task<Result<PropertyResult>> HandleAsync(CreatePropertyCommand command, CancellationToken cancellationToken = default)
    {
        var error = PropertyValidation.Validate(command.Name, command.ProvinceCode, command.WardCode,
            command.AddressDetail, command.PropertyType);
        if (error is not null) return Result.Failure<PropertyResult>(error);
        error = PropertyValidation.ValidateCoordinates(command.Latitude, command.Longitude, required: true);
        if (error is not null) return Result.Failure<PropertyResult>(error);
        var fullAddress = await _properties.GetFullAddressAsync(command.ProvinceCode, command.WardCode,
            command.AddressDetail, cancellationToken);
        if (fullAddress is null) return Result.Failure<PropertyResult>(PropertiesErrors.InvalidAddress);

        var now = DateTimeOffset.UtcNow;
        var property = new Property
        {
            LandlordId = command.LandlordId,
            Name = command.Name.Trim(),
            ProvinceCode = command.ProvinceCode.Trim(),
            WardCode = command.WardCode.Trim(),
            AddressDetail = command.AddressDetail.Trim(),
            PropertyType = command.PropertyType,
            Latitude = decimal.Round(command.Latitude!.Value, 6),
            Longitude = decimal.Round(command.Longitude!.Value, 6),
            CreatedAt = now,
            UpdatedAt = now
        };
        _properties.Add(property);
        await _properties.SaveChangesAsync(cancellationToken);
        return Result.Success(PropertyMapping.ToResult(property));
    }
}
