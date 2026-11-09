using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Viewings.Domain.Constants;

public static class ViewingErrors
{
    public static readonly Error AuthenticationRequired = new(
        "Viewing.AuthenticationRequired",
        "Chưa xác thực danh tính người dùng.");

    public static readonly Error NotFound = new(
        "Viewing.NotFound",
        "Không tìm thấy lịch hẹn xem phòng yêu cầu.");

    public static readonly Error Forbidden = new(
        "Viewing.Forbidden",
        "Bạn không có quyền truy cập hoặc thực hiện thao tác trên lịch hẹn này.");

    public static readonly Error ListingNotFound = new(
        "Viewing.ListingNotFound",
        "Không tìm thấy tin đăng phòng cho thuê hoặc tin đăng hiện không khả dụng.");

    public static readonly Error SelfBookingNotAllowed = new(
        "Viewing.SelfBookingNotAllowed",
        "Chủ nhà không thể tự đặt lịch xem phòng thuộc quyền sở hữu của chính mình.");

    public static readonly Error LandlordBookingNotAllowed = new(
        "Viewing.LandlordBookingNotAllowed",
        "Tài khoản Chủ nhà không được phép đặt lịch xem phòng.");

    public static readonly Error TimeInPast = new(
        "Viewing.TimeInPast",
        "Thời gian đặt lịch hẹn xem phòng phải nằm ở tương lai.");

    public static readonly Error InvalidDuration = new(
        "Viewing.InvalidDuration",
        "Thời lượng buổi xem phòng phải nằm trong khoảng từ 15 đến 180 phút.");

    public static readonly Error ScheduleConflict = new(
        "Viewing.ScheduleConflict",
        "Căn phòng này đã có một lịch xem phòng khác trùng hoặc chồng lấn thời gian trong khoảng thời lượng dự kiến.");

    public static Error InvalidStatusTransition(string currentStatus, string targetStatus) => new(
        "Viewing.InvalidStatusTransition",
        $"Không thể chuyển trạng thái lịch hẹn từ '{currentStatus}' sang '{targetStatus}'.");
}
