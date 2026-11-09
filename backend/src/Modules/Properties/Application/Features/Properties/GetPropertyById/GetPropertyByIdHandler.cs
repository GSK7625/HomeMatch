using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Properties.GetPropertyById;

public sealed class GetPropertyByIdHandler
{
    private readonly IPropertyRepository _properties;

    public GetPropertyByIdHandler(IPropertyRepository properties)
    {
        _properties = properties;
    }

    public async Task<Result<PropertyResult>> HandleAsync(GetPropertyByIdQuery command, CancellationToken cancellationToken = default)
    {
        var property = await _properties.GetOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken);
        return property is null ? Result.Failure<PropertyResult>(PropertiesErrors.PropertyNotFound)
            : Result.Success(PropertyMapping.ToResult(property));
    }
}
