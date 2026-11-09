using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Properties.GetMyProperties;

public sealed class GetMyPropertiesHandler
{
    private readonly IPropertyRepository _properties;

    public GetMyPropertiesHandler(IPropertyRepository properties)
    {
        _properties = properties;
    }

    public async Task<Result<IReadOnlyList<PropertyResult>>> HandleAsync(GetMyPropertiesQuery command, CancellationToken cancellationToken = default)
    {
        return Result.Success(await _properties.GetMineAsync(command.LandlordId, cancellationToken));
    }
}
