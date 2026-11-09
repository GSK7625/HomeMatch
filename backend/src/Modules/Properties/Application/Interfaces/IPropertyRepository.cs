using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Domain.Entities;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface IPropertyRepository
{
    Task<Property?> GetOwnedAsync(long landlordId, long propertyId, CancellationToken cancellationToken);
    Task<bool> IsOwnedAsync(long landlordId, long propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyResult>> GetMineAsync(long landlordId, CancellationToken cancellationToken);
    Task<string?> GetFullAddressAsync(string provinceCode, string wardCode, string addressDetail, CancellationToken cancellationToken);
    Task<IReadOnlyList<long>> GetRoomIdsAsync(long propertyId, CancellationToken cancellationToken);
    void Add(Property property);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
