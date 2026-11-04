using System.Net.Mail;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Auth.Register;

public static class RegisterValidator
{
    public static Error? Validate(RegisterCommand command)
    {
        var details = new List<ErrorDetail>();

        // 1. Validate Email (BR-05)
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            details.Add(new ErrorDetail(nameof(command.Email), "Email không được để trống."));
        }
        else
        {
            var trimmedEmail = command.Email.Trim();
            if (trimmedEmail.Length > 254)
            {
                details.Add(new ErrorDetail(nameof(command.Email), "Email không được vượt quá 254 ký tự."));
            }
            else
            {
                try
                {
                    var addr = new MailAddress(trimmedEmail);
                    if (addr.Address != trimmedEmail)
                    {
                        details.Add(new ErrorDetail(nameof(command.Email), "Định dạng email không hợp lệ."));
                    }
                }
                catch
                {
                    details.Add(new ErrorDetail(nameof(command.Email), "Định dạng email không hợp lệ."));
                }
            }
        }

        // 2. Validate Password (BR-06: 8-128 chars, requires letter and digit)
        if (string.IsNullOrEmpty(command.Password))
        {
            details.Add(new ErrorDetail(nameof(command.Password), "Mật khẩu không được để trống."));
        }
        else
        {
            if (command.Password.Length < 8 || command.Password.Length > 128)
            {
                details.Add(new ErrorDetail(nameof(command.Password), "Mật khẩu phải có độ dài từ 8 đến 128 ký tự."));
            }

            var hasLetter = command.Password.Any(char.IsLetter);
            var hasDigit = command.Password.Any(char.IsDigit);

            if (!hasLetter || !hasDigit)
            {
                details.Add(new ErrorDetail(nameof(command.Password), "Mật khẩu phải chứa ít nhất một chữ cái và một chữ số."));
            }
        }

        // 3. Validate FullName (BR-07: 2-100 chars, no control characters)
        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            details.Add(new ErrorDetail(nameof(command.FullName), "Họ tên không được để trống."));
        }
        else
        {
            var trimmedName = command.FullName.Trim();
            if (trimmedName.Length < 2 || trimmedName.Length > 100)
            {
                details.Add(new ErrorDetail(nameof(command.FullName), "Họ tên phải có độ dài từ 2 đến 100 ký tự."));
            }

            if (trimmedName.Any(char.IsControl))
            {
                details.Add(new ErrorDetail(nameof(command.FullName), "Họ tên không được chứa ký tự điều khiển."));
            }
        }

        // 4. Validate Role (BR-04: Tenant or Landlord only)
        if (string.IsNullOrWhiteSpace(command.Role) ||
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
