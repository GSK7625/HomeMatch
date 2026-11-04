using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public sealed record ResendConfirmationRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    public string Email { get; init; } = string.Empty;
}
