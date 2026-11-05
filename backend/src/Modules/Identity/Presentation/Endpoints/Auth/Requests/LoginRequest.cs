using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; init; } = string.Empty;
}
