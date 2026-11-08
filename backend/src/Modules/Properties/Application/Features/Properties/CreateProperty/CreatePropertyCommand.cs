namespace HomeMatch.Modules.Properties.Application.Features.Properties.CreateProperty;

public sealed record CreatePropertyCommand(
    long LandlordId,
    string Name,
    string ProvinceCode,
    string WardCode,
    string AddressDetail,
    string PropertyType);
