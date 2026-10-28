namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface IPropertyOwnershipReader
{
    Task EnsureOwnedAsync(long landlordId, long propertyId, CancellationToken cancellationToken);
}
