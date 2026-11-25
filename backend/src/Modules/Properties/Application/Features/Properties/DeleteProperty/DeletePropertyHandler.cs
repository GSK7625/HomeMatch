using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Properties.DeleteProperty;

public sealed class DeletePropertyHandler(IPropertyRepository properties)
{
    public async Task<Result> HandleAsync(DeletePropertyCommand command, CancellationToken cancellationToken = default)
    {
        var property = await properties.GetOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken);
        if (property is null) return Result.Failure(PropertiesErrors.PropertyNotFound);
        if ((await properties.GetRoomIdsAsync(property.Id, cancellationToken)).Count != 0)
            return Result.Failure(PropertiesErrors.PropertyHasRooms);
        property.DeletedAt = DateTimeOffset.UtcNow;
        property.UpdatedAt = property.DeletedAt.Value;
        await properties.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
