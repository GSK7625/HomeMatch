using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence;

public sealed class PropertyOwnershipReader(PropertiesDbContext db) : IPropertyOwnershipReader
{
    public async Task EnsureOwnedAsync(long landlordId, long propertyId,
        CancellationToken cancellationToken)
    {
        if (!await db.Properties.AsNoTracking().AnyAsync(
                property => property.Id == propertyId && property.LandlordId == landlordId,
                cancellationToken))
            throw ApiException.NotFound("Property.NotFound", "Không tìm thấy căn thuộc tài khoản này.");
    }
}
