using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;

/// <summary>Request DTO để tạo mới phòng</summary>
public sealed record CreateRoomRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string RoomNumber { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Floor { get; init; }

    [Range(typeof(decimal), "0.01", "9999.99", ParseLimitsInInvariantCulture = true)]
    public decimal AreaSqm { get; init; }

    [Required]
    [RegularExpression("^(SINGLE|STUDIO|1B1B|2B1B|DORMITORY)$")]
    public string RoomType { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxOccupants { get; init; }

    public DateOnly? AvailableFrom { get; init; }

    /// <summary>Danh sách ID tiện ích gán cho phòng ngay khi tạo (tùy chọn)</summary>
    public IReadOnlyList<long>? AmenityIds { get; init; }
}

/// <summary>Request DTO để cập nhật một phần phòng (PATCH)</summary>
public sealed record UpdateRoomRequest
{
    [StringLength(50, MinimumLength = 1)]
    public string? RoomNumber { get; init; }

    [Range(0, int.MaxValue)]
    public int? Floor { get; init; }

    [Range(typeof(decimal), "0.01", "9999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? AreaSqm { get; init; }

    [RegularExpression("^(SINGLE|STUDIO|1B1B|2B1B|DORMITORY)$")]
    public string? RoomType { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaxOccupants { get; init; }

    public DateOnly? AvailableFrom { get; init; }

    /// <summary>Danh sách ID tiện ích cập nhật cho phòng (tùy chọn)</summary>
    public IReadOnlyList<long>? AmenityIds { get; init; }
}

/// <summary>Request DTO để thay đổi trạng thái phòng</summary>
public sealed record SetRoomStatusRequest
{
    [Required]
    [RegularExpression("^(AVAILABLE|RENTED|SUSPENDED)$")]
    public string Status { get; init; } = string.Empty;
}
