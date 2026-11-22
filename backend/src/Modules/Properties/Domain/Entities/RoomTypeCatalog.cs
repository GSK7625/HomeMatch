namespace HomeMatch.Modules.Properties.Domain.Entities;

/// <summary>
/// Thực thể Danh mục Loại phòng (RoomTypeCatalog) phục vụ tạo phòng và bộ lọc tìm kiếm.
/// </summary>
public sealed class RoomTypeCatalog
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
