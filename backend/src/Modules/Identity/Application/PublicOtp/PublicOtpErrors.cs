using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.PublicOtp;

public static class PublicOtpErrors
{
    public static readonly Error Invalid = new("Identity.OtpInvalid", "Mã xác thực hoặc quyền đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
    public static readonly Error Validation = new("Identity.ValidationFailed", "Dữ liệu yêu cầu không hợp lệ.");
    public static readonly Error Unavailable = new("Server.DependencyUnavailable", "Dịch vụ tạm thời chưa sẵn sàng.");
}
