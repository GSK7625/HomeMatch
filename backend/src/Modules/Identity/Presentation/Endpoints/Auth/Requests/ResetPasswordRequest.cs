using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public sealed record ResetPasswordRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "Định danh người dùng không hợp lệ.")]
    public long UserId { get; init; }

    [Required(ErrorMessage = "Mã token đặt lại mật khẩu không được để trống.")]
    public string Token { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải có độ dài từ 8 đến 128 ký tự.")]
    public string NewPassword { get; init; } = string.Empty;
}
