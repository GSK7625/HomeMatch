using System.Text.RegularExpressions;
using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Identity.Application.Features.Profile.UpdateProfile;

public static class UpdateProfileValidator
{
    private static readonly HashSet<string> AllowedGenders = new(StringComparer.OrdinalIgnoreCase)
    {
        "MALE",
        "FEMALE",
        "OTHER"
    };

    private static readonly Regex VietnamPhoneRegex = new(@"^(0|\+84)[0-9]{9,10}$", RegexOptions.Compiled);

    public static Error? Validate(UpdateProfileCommand command)
    {
        var details = new List<ErrorDetail>();

        if (command.UserId <= 0)
        {
            details.Add(new ErrorDetail(nameof(command.UserId), "Định danh người dùng không hợp lệ."));
        }

        var fullName = command.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            details.Add(new ErrorDetail("fullName", "Họ và tên không được để trống."));
        }
        else if (fullName.Length < 2 || fullName.Length > 100)
        {
            details.Add(new ErrorDetail("fullName", "Họ và tên phải có độ dài từ 2 đến 100 ký tự."));
        }

        if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            var normalizedPhone = command.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
            if (!VietnamPhoneRegex.IsMatch(normalizedPhone))
            {
                details.Add(new ErrorDetail("phoneNumber", "Số điện thoại không hợp lệ (ví dụ: 0912345678 hoặc +84912345678)."));
            }
        }

        if (command.DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            if (command.DateOfBirth.Value > today)
            {
                details.Add(new ErrorDetail("dateOfBirth", "Ngày sinh không được lớn hơn ngày hiện tại."));
            }
            else if (command.DateOfBirth.Value < new DateOnly(1900, 1, 1))
            {
                details.Add(new ErrorDetail("dateOfBirth", "Ngày sinh không hợp lệ."));
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Gender) && !AllowedGenders.Contains(command.Gender.Trim()))
        {
            details.Add(new ErrorDetail("gender", "Giới tính chỉ chấp nhận MALE, FEMALE hoặc OTHER."));
        }

        if (details.Count > 0)
        {
            return Error.Validation(
                code: IdentityErrors.ValidationFailed,
                message: "Thông tin hồ sơ không hợp lệ.",
                details: details);
        }

        return null;
    }
}
