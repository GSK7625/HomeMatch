using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Profile.ChangePassword;

public static class ChangePasswordValidator
{
    public static Error? Validate(ChangePasswordCommand command)
    {
        var details = new List<ErrorDetail>();

        if (command.UserId <= 0)
        {
            details.Add(new ErrorDetail(nameof(command.UserId), "Định danh người dùng không hợp lệ."));
        }

        if (string.IsNullOrEmpty(command.CurrentPassword))
        {
            details.Add(new ErrorDetail("currentPassword", "Vui lòng nhập mật khẩu hiện tại."));
        }

        // BR-06: Mật khẩu mới 8-128 ký tự, có ít nhất 1 chữ cái và 1 chữ số
        if (string.IsNullOrEmpty(command.NewPassword))
        {
            details.Add(new ErrorDetail("newPassword", "Vui lòng nhập mật khẩu mới."));
        }
        else
        {
            if (command.NewPassword.Length < 8 || command.NewPassword.Length > 128)
            {
                details.Add(new ErrorDetail("newPassword", "Mật khẩu mới phải có độ dài từ 8 đến 128 ký tự."));
            }

            var hasLetter = command.NewPassword.Any(char.IsLetter);
            var hasDigit = command.NewPassword.Any(char.IsDigit);
            if (!hasLetter || !hasDigit)
            {
                details.Add(new ErrorDetail("newPassword", "Mật khẩu mới phải chứa ít nhất một chữ cái và một chữ số."));
            }

            if (!string.IsNullOrEmpty(command.CurrentPassword) && command.NewPassword == command.CurrentPassword)
            {
                details.Add(new ErrorDetail("newPassword", "Mật khẩu mới không được trùng với mật khẩu hiện tại."));
            }
        }

        if (string.IsNullOrEmpty(command.ConfirmNewPassword))
        {
            details.Add(new ErrorDetail("confirmNewPassword", "Vui lòng xác nhận mật khẩu mới."));
        }
        else if (command.ConfirmNewPassword != command.NewPassword)
        {
            details.Add(new ErrorDetail("confirmNewPassword", "Mật khẩu xác nhận không khớp với mật khẩu mới."));
        }

        if (details.Count > 0)
        {
            return Error.Validation(
                code: IdentityErrors.ValidationFailed,
                message: "Dữ liệu đổi mật khẩu không hợp lệ.",
                details: details);
        }

        return null;
    }
}
