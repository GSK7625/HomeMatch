using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Properties.Application.DTOs;

/// <summary>Request DTO để tạo mới phòng</summary>
public sealed class CreateRoomRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string RoomNumber { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Floor { get; init; }

    [Range(typeof(decimal), "0.01", "9999.99", ParseLimitsInInvariantCulture = true)]
    public decimal AreaSqm { get; init; }

    [Required]
    [RegularExpression("^(STUDIO|1PN|2PN|DUPLEX|SHARED)$")]
    public string RoomType { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxOccupants { get; init; }

    [Range(typeof(long), "0", "9223372036854775807")]
    public long BasePrice { get; init; }

    public DateOnly? AvailableFrom { get; init; }
}

/// <summary>Request DTO để cập nhật một phần phòng (PATCH)</summary>
public sealed class UpdateRoomRequest
{
    [StringLength(50, MinimumLength = 1)]
    public string? RoomNumber { get; init; }

    [Range(0, int.MaxValue)]
    public int? Floor { get; init; }

    [Range(typeof(decimal), "0.01", "9999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? AreaSqm { get; init; }

    [RegularExpression("^(STUDIO|1PN|2PN|DUPLEX|SHARED)$")]
    public string? RoomType { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaxOccupants { get; init; }

    [Range(typeof(long), "0", "9223372036854775807")]
    public long? BasePrice { get; init; }

    public DateOnly? AvailableFrom { get; init; }
}

/// <summary>Request DTO để thay đổi trạng thái phòng</summary>
public sealed class SetRoomStatusRequest
{
    [Required]
    [RegularExpression("^(AVAILABLE|RENTED|SUSPENDED)$")]
    public string Status { get; init; } = string.Empty;
}

/// <summary>Request DTO để thiết lập danh sách tiện ích của phòng</summary>
public sealed class SetRoomAmenitiesRequest
{
    [Required]
    public IReadOnlyList<long> AmenityIds { get; init; } = null!;
}

/// <summary>Response DTO trả về thông tin phòng</summary>
public sealed class RoomResponse(
    long id, long propertyId, string roomNumber, int floor, decimal areaSqm,
    string roomType, int maxOccupants, long basePrice, string status,
    DateOnly availableFrom, IReadOnlyList<long> amenityIds)
{
    public long Id { get; } = id;
    public long PropertyId { get; } = propertyId;
    public string RoomNumber { get; } = roomNumber;
    public int Floor { get; } = floor;
    public decimal AreaSqm { get; } = areaSqm;
    public string RoomType { get; } = roomType;
    public int MaxOccupants { get; } = maxOccupants;
    public long BasePrice { get; } = basePrice;
    public string Status { get; } = status;
    public DateOnly AvailableFrom { get; } = availableFrom;
    public IReadOnlyList<long> AmenityIds { get; } = amenityIds;
}
