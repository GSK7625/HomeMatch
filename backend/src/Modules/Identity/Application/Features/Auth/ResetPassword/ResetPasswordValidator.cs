using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.ResetPassword;

public static class ResetPasswordValidator
{
    public static Error? Validate(ResetPasswordCommand command)
    {
        var details = new List<ErrorDetail>();

        // BR-06: Mật khẩu mới bắt buộc phải chứa ít nhất một chữ cái và một chữ số
        if (!string.IsNullOrEmpty(command.NewPassword))
        {
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
