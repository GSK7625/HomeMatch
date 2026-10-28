namespace HomeMatch.Modules.Identity.Domain.Entities;

/// <summary>
/// Thực thể Tài khoản người dùng (User) trong hệ thống HomeMatch
/// </summary>
public sealed class User
{
    /// <summary>Mã định danh duy nhất của Người dùng</summary>
    public long Id { get; set; }

    /// <summary>Địa chỉ Email đăng nhập (Không phân biệt hoa thường - kiểu citext trong PostgreSQL)</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Số điện thoại liên hệ (Có thể null khi đăng ký bằng email)</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Chuỗi băm mật khẩu bảo mật (Password Hash)</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Vai trò người dùng: TENANT (Khách thuê), LANDLORD (Chủ nhà), ADMIN (Quản trị viên sàn)</summary>
    public string Role { get; set; } = "TENANT";

    /// <summary>Trạng thái tài khoản: ACTIVE (Hoạt động), LOCKED (Bị khóa), PENDING_VERIFICATION (Chờ xác minh)</summary>
    public string Status { get; set; } = "ACTIVE";

    /// <summary>Thời điểm tạo tài khoản</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Thời điểm cập nhật thông tin tài khoản gần nhất</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
