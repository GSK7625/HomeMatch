using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.Login;

public static class LoginValidator
{
    public static Error? Validate(LoginCommand command)
    {
        var details = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            details.Add(new ErrorDetail(nameof(command.Email), "Email không được để trống."));
        }

        if (string.IsNullOrEmpty(command.Password))
        {
            details.Add(new ErrorDetail(nameof(command.Password), "Mật khẩu không được để trống."));
        }

        if (details.Count > 0)
        {
            return Error.Validation(
                code: "Identity.ValidationFailed",
                message: "Thông tin đăng nhập không hợp lệ.",
                details: details);
        }

        return null;
    }
}
