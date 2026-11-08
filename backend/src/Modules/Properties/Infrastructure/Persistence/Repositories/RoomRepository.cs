using HomeMatch.Modules.Properties.Application;
using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository(PropertiesDbContext db) : IRoomRepository
{
    public Task<Room?> GetAsync(long propertyId, long roomId, CancellationToken cancellationToken) =>
        db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId && r.PropertyId == propertyId, cancellationToken);

    public async Task<IReadOnlyList<RoomResult>> GetByPropertyAsync(long propertyId, CancellationToken cancellationToken)
    {
        var rooms = await db.Rooms.AsNoTracking().Where(r => r.PropertyId == propertyId)
            .OrderBy(r => r.RoomNumber).ThenBy(r => r.Id).ToListAsync(cancellationToken);
        var ids = rooms.Select(r => r.Id).ToArray();
        var amenities = await db.RoomAmenities.AsNoTracking().Where(a => ids.Contains(a.RoomId))
            .Select(ra => new { ra.RoomId, ra.AmenityId })
            .ToListAsync(cancellationToken);
        var byRoom = amenities.ToLookup(a => a.RoomId, a => a.AmenityId);
        return rooms.Select(room =>
        {
            var roomAmenityIds = byRoom[room.Id].OrderBy(id => id).ToArray();
            return PropertyMapping.ToResult(room, roomAmenityIds);
        }).ToArray();
    }

    public async Task<IReadOnlyList<long>> GetAmenityIdsAsync(long roomId, CancellationToken cancellationToken) =>
        await db.RoomAmenities.AsNoTracking().Where(a => a.RoomId == roomId)
            .OrderBy(a => a.AmenityId).Select(a => a.AmenityId).ToListAsync(cancellationToken);

    /// Đảm bảo số phòng không bị trùng lặp trong cùng tòa nhà
    public Task<bool> NumberExistsAsync(long propertyId, string number, long? exceptId, CancellationToken cancellationToken) =>
        db.Rooms.AnyAsync(r => r.PropertyId == propertyId && r.RoomNumber == number && r.Id != exceptId, cancellationToken);

    public async Task<bool> AmenitiesExistAsync(IReadOnlyList<long> amenityIds, CancellationToken cancellationToken) =>
        await db.Amenities.CountAsync(a => amenityIds.Contains(a.Id), cancellationToken) == amenityIds.Count;

    public async Task ReplaceAmenitiesAsync(long roomId, IReadOnlyList<long> amenityIds, CancellationToken cancellationToken)
    {
        var existing = await db.RoomAmenities.Where(a => a.RoomId == roomId).ToListAsync(cancellationToken);
        var wantedIds = amenityIds.ToHashSet();
        db.RoomAmenities.RemoveRange(existing.Where(a => !wantedIds.Contains(a.AmenityId)));
        var existingIds = existing.Select(a => a.AmenityId).ToHashSet();
        db.RoomAmenities.AddRange(amenityIds.Where(id => !existingIds.Contains(id))
            .Select(id => new RoomAmenity { RoomId = roomId, AmenityId = id }));
    }

    public void Add(Room room) => db.Rooms.Add(room);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation } postgres
            && postgres.ConstraintName is "ux_rooms_property_number" or "uq_rooms_property_number")
        {
            return Result.Failure(PropertiesErrors.DuplicateRoomNumber);
        }
    }
}
