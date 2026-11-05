using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public sealed record RegisterRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có độ dài từ 8 đến 128 ký tự.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải có độ dài từ 2 đến 100 ký tự.")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vai trò không được để trống.")]
    public string Role { get; init; } = string.Empty;
}
