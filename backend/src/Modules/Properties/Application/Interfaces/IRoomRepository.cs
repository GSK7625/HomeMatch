using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetAsync(long propertyId, long roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomResult>> GetByPropertyAsync(long propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<long>> GetAmenityIdsAsync(long roomId, CancellationToken cancellationToken);
    Task<bool> NumberExistsAsync(long propertyId, string number, long? exceptId, CancellationToken cancellationToken);
    Task<bool> AmenitiesExistAsync(IReadOnlyList<long> amenityIds, CancellationToken cancellationToken);
    Task ReplaceAmenitiesAsync(long roomId, IReadOnlyList<long> amenityIds, CancellationToken cancellationToken);
    void Add(Room room);
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
