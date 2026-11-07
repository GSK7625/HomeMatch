using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.ResetPassword;

public static class ResetPasswordValidator
{
    public static Error? Validate(ResetPasswordCommand command)
    {
        var details = new List<ErrorDetail>();

        if (command.UserId <= 0)
        {
            details.Add(new ErrorDetail(nameof(command.UserId), "Định danh người dùng không hợp lệ."));
        }

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            details.Add(new ErrorDetail(nameof(command.Token), "Mã token đặt lại mật khẩu không được để trống."));
        }

        // BR-06: Mật khẩu mới 8-128 ký tự, có ít nhất chữ cái và chữ số
        if (string.IsNullOrEmpty(command.NewPassword))
        {
            details.Add(new ErrorDetail(nameof(command.NewPassword), "Mật khẩu mới không được để trống."));
        }
        else
        {
            if (command.NewPassword.Length < 8 || command.NewPassword.Length > 128)
            {
                details.Add(new ErrorDetail(nameof(command.NewPassword), "Mật khẩu mới phải có độ dài từ 8 đến 128 ký tự."));
            }

            var hasLetter = command.NewPassword.Any(char.IsLetter);
            var hasDigit = command.NewPassword.Any(char.IsDigit);

            if (!hasLetter || !hasDigit)
            {
                details.Add(new ErrorDetail(nameof(command.NewPassword), "Mật khẩu mới phải chứa ít nhất một chữ cái và một chữ số."));
            }
        }

        if (details.Count > 0)
        {
            return Error.Validation(
                code: IdentityErrors.ValidationFailed,
                message: "Dữ liệu đặt lại mật khẩu không hợp lệ.",
                details: details);
        }

        return null;
    }
}
