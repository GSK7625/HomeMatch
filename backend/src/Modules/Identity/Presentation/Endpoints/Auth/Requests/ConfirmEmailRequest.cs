using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public sealed record ConfirmEmailRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "Định danh người dùng không hợp lệ.")]
    public long UserId { get; init; }

    [Required(ErrorMessage = "Mã token xác thực không được để trống.")]
    public string Token { get; init; } = string.Empty;
}
