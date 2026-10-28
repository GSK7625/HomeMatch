using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Modules.Properties.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Application;

/// <summary>
/// Dịch vụ quản lý Phòng trọ (Room) và Tiện ích phòng (RoomAmenity).
/// Tuân thủ SRP — chỉ xử lý nghiệp vụ liên quan đến Room, tách biệt khỏi PropertyService.
/// Tuân thủ DIP — implement IRoomService, được inject qua interface.
/// </summary>
public sealed class RoomService(
    PropertiesDbContext db,
    IPropertyOwnershipReader propertyOwnership,
    IListingVisibilityWriter listingVisibility) : IRoomService
{
    private static readonly HashSet<string> ValidRoomTypes =
        ["STUDIO", "1PN", "2PN", "DUPLEX", "SHARED"];

    private static readonly HashSet<string> ValidRoomStatuses =
        ["AVAILABLE", "RENTED", "SUSPENDED"];

    /// Tạo mới phòng trọ trong tòa nhà
    public async Task<RoomResponse> CreateAsync(
        long landlordId, long propertyId, CreateRoomRequest request, CancellationToken cancellationToken)
    {
        await propertyOwnership.EnsureOwnedAsync(landlordId, propertyId, cancellationToken);
        Validate(request.RoomNumber, request.Floor, request.AreaSqm,
            request.RoomType, request.MaxOccupants, request.BasePrice);
        await EnsureRoomNumberAvailableAsync(propertyId, request.RoomNumber.Trim(), null, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var room = new Room
        {
            PropertyId = propertyId,
            RoomNumber = request.RoomNumber.Trim(),
            Floor = request.Floor,
            AreaSqm = request.AreaSqm,
            RoomType = request.RoomType,
            MaxOccupants = request.MaxOccupants,
            BasePrice = request.BasePrice,
            Status = "AVAILABLE",
            AvailableFrom = request.AvailableFrom ?? BusinessDate.Today(),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(room, []);
    }

    /// Lấy danh sách tất cả phòng trong tòa nhà kèm tiện ích
    public async Task<IReadOnlyList<RoomResponse>> GetByPropertyAsync(
        long landlordId, long propertyId, CancellationToken cancellationToken)
    {
        await propertyOwnership.EnsureOwnedAsync(landlordId, propertyId, cancellationToken);
        var rooms = await db.Rooms.AsNoTracking()
            .Where(r => r.PropertyId == propertyId)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync(cancellationToken);

        var ids = rooms.Select(r => r.Id).ToArray();
        var amenities = await db.RoomAmenities.AsNoTracking()
            .Where(a => ids.Contains(a.RoomId))
            .ToListAsync(cancellationToken);

        return rooms.Select(r => ToResponse(r,
            amenities.Where(a => a.RoomId == r.Id)
                     .Select(a => a.AmenityId).ToArray()))
            .ToArray();
    }

    /// Xem chi tiết 1 phòng trọ kèm tiện ích
    public async Task<RoomResponse> GetByIdAsync(
        long landlordId, long propertyId, long roomId, CancellationToken cancellationToken)
    {
        var room = await GetOwnedRoomAsync(landlordId, propertyId, roomId, cancellationToken);
        return await ToResponseWithAmenitiesAsync(room, cancellationToken);
    }

    /// Cập nhật thông tin phòng trọ (tự động ẩn tin đăng nếu có thay đổi)
    public async Task<RoomResponse> UpdateAsync(
        long landlordId, long propertyId, long roomId, UpdateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var room = await GetOwnedRoomAsync(landlordId, propertyId, roomId, cancellationToken);
        var number = (request.RoomNumber ?? room.RoomNumber).Trim();
        var floor = request.Floor ?? room.Floor;
        var area = request.AreaSqm ?? room.AreaSqm;
        var type = request.RoomType ?? room.RoomType;
        var occupants = request.MaxOccupants ?? room.MaxOccupants;
        var price = request.BasePrice ?? room.BasePrice;

        Validate(number, floor, area, type, occupants, price);

        if (number != room.RoomNumber)
            await EnsureRoomNumberAvailableAsync(propertyId, number, roomId, cancellationToken);

        var hasChanges = number != room.RoomNumber || floor != room.Floor
            || area != room.AreaSqm || type != room.RoomType
            || occupants != room.MaxOccupants || price != room.BasePrice
            || request.AvailableFrom is not null && request.AvailableFrom != room.AvailableFrom;

        if (hasChanges)
            await listingVisibility.HidePublishedForRoomAsync(roomId, cancellationToken);

        room.RoomNumber = number;
        room.Floor = floor;
        room.AreaSqm = area;
        room.RoomType = type;
        room.MaxOccupants = occupants;
        room.BasePrice = price;
        room.AvailableFrom = request.AvailableFrom ?? room.AvailableFrom;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await ToResponseWithAmenitiesAsync(room, cancellationToken);
    }

    /// Thay đổi trạng thái phòng (AVAILABLE, RENTED, SUSPENDED)
    public async Task<RoomResponse> SetStatusAsync(
        long landlordId, long propertyId, long roomId, SetRoomStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidRoomStatuses.Contains(request.Status))
            throw ApiException.BadRequest("Room.InvalidStatus", "Trạng thái phòng không hợp lệ.");

        var room = await GetOwnedRoomAsync(landlordId, propertyId, roomId, cancellationToken);
        room.Status = request.Status;
        room.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (request.Status != "AVAILABLE")
            await listingVisibility.HidePublishedForRoomAsync(roomId, cancellationToken);

        return await ToResponseWithAmenitiesAsync(room, cancellationToken);
    }

    /// Cập nhật danh sách tiện ích của phòng
    public async Task<RoomResponse> SetAmenitiesAsync(
        long landlordId, long propertyId, long roomId, SetRoomAmenitiesRequest request,
        CancellationToken cancellationToken)
    {
        var room = await GetOwnedRoomAsync(landlordId, propertyId, roomId, cancellationToken);

        if (request.AmenityIds is null
            || request.AmenityIds.Any(id => id <= 0)
            || request.AmenityIds.Distinct().Count() != request.AmenityIds.Count)
            throw ApiException.BadRequest("Room.InvalidAmenities", "Danh sách tiện ích không hợp lệ.");

        var count = await db.Amenities.CountAsync(
            a => request.AmenityIds.Contains(a.Id), cancellationToken);
        if (count != request.AmenityIds.Count)
            throw ApiException.BadRequest("Room.InvalidAmenities", "Có tiện ích không tồn tại.");

        var existing = await db.RoomAmenities
            .Where(a => a.RoomId == roomId)
            .ToListAsync(cancellationToken);

        var wantedIds = request.AmenityIds.ToHashSet();
        db.RoomAmenities.RemoveRange(existing.Where(a => !wantedIds.Contains(a.AmenityId)));

        var existingIds = existing.Select(a => a.AmenityId).ToHashSet();
        db.RoomAmenities.AddRange(
            request.AmenityIds
                .Where(id => !existingIds.Contains(id))
                .Select(id => new RoomAmenity { RoomId = roomId, AmenityId = id }));

        room.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(room, request.AmenityIds);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// Kiểm tra quyền sở hữu phòng của chủ nhà
    private async Task<Room> GetOwnedRoomAsync(
        long landlordId, long propertyId, long roomId, CancellationToken cancellationToken)
    {
        await propertyOwnership.EnsureOwnedAsync(landlordId, propertyId, cancellationToken);
        return await db.Rooms.FirstOrDefaultAsync(
            r => r.Id == roomId && r.PropertyId == propertyId, cancellationToken)
            ?? throw ApiException.NotFound("Room.NotFound", "Không tìm thấy phòng trong căn này.");
    }

    /// Đóng gói đối tượng RoomResponse kèm danh sách ID tiện ích
    private async Task<RoomResponse> ToResponseWithAmenitiesAsync(
        Room room, CancellationToken cancellationToken)
    {
        var amenities = await db.RoomAmenities.AsNoTracking()
            .Where(a => a.RoomId == room.Id)
            .Select(a => a.AmenityId)
            .ToListAsync(cancellationToken);
        return ToResponse(room, amenities);
    }

    /// Đảm bảo số phòng không bị trùng lặp trong cùng tòa nhà
    private async Task EnsureRoomNumberAvailableAsync(
        long propertyId, string number, long? exceptRoomId, CancellationToken cancellationToken)
    {
        if (await db.Rooms.AnyAsync(
            r => r.PropertyId == propertyId && r.RoomNumber == number && r.Id != exceptRoomId,
            cancellationToken))
            throw ApiException.Conflict("Room.DuplicateNumber", "Số phòng đã tồn tại trong căn này.");
    }

    /// Kiểm tra tính hợp lệ của các thông số phòng
    private static void Validate(
        string number, int floor, decimal area, string type, int occupants, long price)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Length > 50
            || floor < 0 || area <= 0 || area > 9999.99m
            || !ValidRoomTypes.Contains(type)
            || occupants <= 0 || price < 0)
            throw ApiException.BadRequest("Room.InvalidInput", "Thông tin phòng không hợp lệ.");
    }

    private static RoomResponse ToResponse(Room room, IReadOnlyList<long> amenityIds) =>
        new(room.Id, room.PropertyId, room.RoomNumber, room.Floor, room.AreaSqm,
            room.RoomType, room.MaxOccupants, room.BasePrice, room.Status,
            room.AvailableFrom, amenityIds);
}
