namespace HomeMatch.Modules.Properties.Application.Features.Properties.UpdateProperty;

public sealed record UpdatePropertyCommand(
    long LandlordId,
    long PropertyId,
    string? Name,
    string? ProvinceCode,
    string? WardCode,
    string? AddressDetail,
    string? PropertyType);
