using HomeMatch.Modules.Properties.Application;
using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Repositories;

public sealed class AdminCatalogRepository(PropertiesDbContext db) : IAdminCatalogRepository
{
    public async Task<IReadOnlyList<AmenityResult>> GetAllAmenitiesAsync(CancellationToken cancellationToken) =>
        await db.Amenities.AsNoTracking()
            .OrderBy(a => a.Id)
            .Select(a => new AmenityResult(a.Id, a.Code, a.Name, a.IsActive))
            .ToListAsync(cancellationToken);

    public Task<Amenity?> GetAmenityByIdAsync(long id, CancellationToken cancellationToken) =>
        db.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> AmenityCodeExistsAsync(string code, long? exceptId, CancellationToken cancellationToken) =>
        db.Amenities.AnyAsync(a => a.Code == code && (!exceptId.HasValue || a.Id != exceptId.Value), cancellationToken);

    public Task<bool> AmenityNameExistsAsync(string name, long? exceptId, CancellationToken cancellationToken) =>
        db.Amenities.AnyAsync(a => a.Name == name && (!exceptId.HasValue || a.Id != exceptId.Value), cancellationToken);

    public void AddAmenity(Amenity amenity) => db.Amenities.Add(amenity);

    public async Task<IReadOnlyList<RoomTypeResult>> GetAllRoomTypesAsync(CancellationToken cancellationToken) =>
        await db.RoomTypes.AsNoTracking()
            .OrderBy(rt => rt.Id)
            .Select(rt => new RoomTypeResult(rt.Id, rt.Code, rt.Name, rt.IsActive))
            .ToListAsync(cancellationToken);

    public Task<RoomTypeCatalog?> GetRoomTypeByIdAsync(long id, CancellationToken cancellationToken) =>
        db.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);

    public Task<bool> RoomTypeCodeExistsAsync(string code, long? exceptId, CancellationToken cancellationToken) =>
        db.RoomTypes.AnyAsync(rt => rt.Code == code && (!exceptId.HasValue || rt.Id != exceptId.Value), cancellationToken);

    public Task<bool> RoomTypeNameExistsAsync(string name, long? exceptId, CancellationToken cancellationToken) =>
        db.RoomTypes.AnyAsync(rt => rt.Name == name && (!exceptId.HasValue || rt.Id != exceptId.Value), cancellationToken);

    public void AddRoomType(RoomTypeCatalog roomType) => db.RoomTypes.Add(roomType);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Result.Failure(PropertiesErrors.DuplicateCatalogEntry);
        }
    }
}
