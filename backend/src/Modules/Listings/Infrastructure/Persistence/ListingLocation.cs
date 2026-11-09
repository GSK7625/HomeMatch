using HomeMatch.Modules.Listings.Domain.Entities;
using NetTopologySuite.Geometries;

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence;

/// <summary>
/// Thực thể Tọa độ địa lý không gian của Tin đăng (ListingLocation)
/// Sử dụng công nghệ PostGIS Point(Kinh độ, Vĩ độ) với hệ quy chiếu WGS 84 (SRID 4326)
/// </summary>
public sealed class ListingLocation
{
    /// <summary>ID tin đăng liên kết (Vừa là Khóa chính vừa là Khóa ngoại 1-1 với bảng listings)</summary>
    public long ListingId { get; set; }

    /// <summary>Tọa độ địa lý không gian PostGIS (Kinh độ/Longitude, Vĩ độ/Latitude)</summary>
    public Point? Coordinates { get; set; }

    /// <summary>Đối tượng Tin đăng sở hữu tọa độ này</summary>
    public Listing Listing { get; set; } = null!;
}
