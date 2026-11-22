using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

public interface IAdminCatalogRepository
{
    Task<IReadOnlyList<AmenityResult>> GetAllAmenitiesAsync(CancellationToken cancellationToken);
    Task<Amenity?> GetAmenityByIdAsync(long id, CancellationToken cancellationToken);
    Task<bool> AmenityCodeExistsAsync(string code, long? exceptId, CancellationToken cancellationToken);
    Task<bool> AmenityNameExistsAsync(string name, long? exceptId, CancellationToken cancellationToken);
    void AddAmenity(Amenity amenity);

    Task<IReadOnlyList<RoomTypeResult>> GetAllRoomTypesAsync(CancellationToken cancellationToken);
    Task<RoomTypeCatalog?> GetRoomTypeByIdAsync(long id, CancellationToken cancellationToken);
    Task<bool> RoomTypeCodeExistsAsync(string code, long? exceptId, CancellationToken cancellationToken);
    Task<bool> RoomTypeNameExistsAsync(string name, long? exceptId, CancellationToken cancellationToken);
    void AddRoomType(RoomTypeCatalog roomType);

    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
