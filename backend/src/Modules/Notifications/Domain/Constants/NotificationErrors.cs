using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Notifications.Domain.Constants;

public static class NotificationErrors
{
    public static readonly Error AuthenticationRequired = new(
        "Notification.AuthenticationRequired",
        "Chưa xác thực danh tính người dùng.");

    public static readonly Error NotFound = new(
        "Notification.NotFound",
        "Không tìm thấy thông báo yêu cầu.");

    public static readonly Error Forbidden = new(
        "Notification.Forbidden",
        "Bạn không có quyền truy cập thông báo này.");
}
