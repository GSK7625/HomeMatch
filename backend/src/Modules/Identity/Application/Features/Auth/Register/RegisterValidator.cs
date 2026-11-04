using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.Register;

public static class RegisterValidator
{
    public static Error? Validate(RegisterCommand command)
    {
        var details = new List<ErrorDetail>();

        // 1. Validate Password (BR-06): Phải chứa ít nhất một chữ cái và một chữ số
        if (!string.IsNullOrEmpty(command.Password))
        {
            var hasLetter = command.Password.Any(char.IsLetter);
            var hasDigit = command.Password.Any(char.IsDigit);

            if (!hasLetter || !hasDigit)
            {
                details.Add(new ErrorDetail(nameof(command.Password), "Mật khẩu phải chứa ít nhất một chữ cái và một chữ số."));
            }
        }

        // 2. Validate FullName (BR-07): Không được chứa ký tự điều khiển
        if (!string.IsNullOrEmpty(command.FullName) && command.FullName.Any(char.IsControl))
        {
            details.Add(new ErrorDetail(nameof(command.FullName), "Họ tên không được chứa ký tự điều khiển."));
        }

        // 3. Validate Role (BR-04): Chỉ chấp nhận Tenant hoặc Landlord
        if (!string.IsNullOrEmpty(command.Role) &&
            !AccountRoles.PublicAssignableRoles.Contains(command.Role.Trim()))
        {
            details.Add(new ErrorDetail(nameof(command.Role), $"Vai trò đăng ký không hợp lệ. Chỉ chấp nhận '{AccountRoles.Tenant}' hoặc '{AccountRoles.Landlord}'."));
        }

        if (details.Count > 0)
        {
            return Error.Validation(
                code: "Identity.ValidationFailed",
                message: "Dữ liệu đăng ký không hợp lệ.",
                details: details);
        }

        return null;
    }
}
