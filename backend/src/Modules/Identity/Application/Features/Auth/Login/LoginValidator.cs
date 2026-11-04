using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.Login;

public static class LoginValidator
{
    public static Error? Validate(LoginCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrEmpty(command.Password))
        {
            return Error.Validation(
                code: "Identity.ValidationFailed",
                message: "Thông tin đăng nhập không hợp lệ.",
                details: new[]
                {
                    new ErrorDetail("Login", "Email và mật khẩu không được để trống.")
                });
        }

        return null;
    }
}
