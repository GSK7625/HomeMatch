using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation;

public static class IdentityHttpErrors
{
    public static IResult ValidationFailed() => Failure(400, "Identity.ValidationFailed", "Dữ liệu yêu cầu không hợp lệ.");
    public static IResult RequestTooLarge() => Failure(413, "Identity.RequestTooLarge", "Nội dung yêu cầu vượt quá giới hạn cho phép.");
    public static IResult UnsupportedMediaType() => Failure(415, "Identity.UnsupportedMediaType", "Yêu cầu phải sử dụng JSON UTF-8.");
    public static IResult RateLimited() => Failure(429, "Identity.RateLimitExceeded", "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.");
    public static IResult SessionInvalid() => Failure(401, "Identity.SessionInvalid", "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
    public static IResult InvalidCredentials() => Failure(401, "Identity.InvalidCredentials", "Email hoặc mật khẩu không hợp lệ.");
    public static IResult AccountSuspended() => Failure(403, "Identity.AccountSuspended", "Tài khoản hiện không được phép đăng nhập.");
    public static IResult AuthenticationRequired() => Failure(401, "Identity.AuthenticationRequired", "Vui lòng đăng nhập để tiếp tục.");
    public static IResult Forbidden() => Failure(403, "Identity.Forbidden", "Bạn không có quyền thực hiện thao tác này.");
    public static IResult OriginNotAllowed() => Failure(403, "Identity.OriginNotAllowed", "Nguồn yêu cầu không được chấp nhận.");
    public static IResult CsrfInvalid() => Failure(403, "Identity.CsrfInvalid", "Yêu cầu bảo mật không hợp lệ. Vui lòng thử lại.");
    public static IResult DependencyUnavailable() => Failure(503, "Server.DependencyUnavailable", "Dịch vụ tạm thời chưa sẵn sàng.");

    public static IResult EmailVerificationInvalid() => Failure(400, "Identity.EmailVerificationInvalid", "Mã xác minh không hợp lệ hoặc đã hết hiệu lực.");

    private static IResult Failure(int status, string code, string message)
        => Results.Json(ApiResponse<object>.Failure(new Error(code, message)), statusCode: status);
}
