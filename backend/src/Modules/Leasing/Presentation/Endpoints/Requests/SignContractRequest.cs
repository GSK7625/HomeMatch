using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

public sealed record SignContractRequest(
    string DocumentHash,
    string OtpCode,
    string SignatureMethod = "OTP_EMAIL")
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (string.IsNullOrWhiteSpace(DocumentHash))
        {
            errors.Add(new ErrorDetail("documentHash", "Mã băm tài liệu ký (documentHash) không được để trống."));
        }
        else if (DocumentHash.Length > 256)
        {
            errors.Add(new ErrorDetail("documentHash", "Mã băm tài liệu ký không được vượt quá 256 ký tự."));
        }

        if (string.IsNullOrWhiteSpace(OtpCode))
        {
            errors.Add(new ErrorDetail("otpCode", "Mã xác thực OTP (otpCode) không được để trống."));
        }
        else if (OtpCode.Trim().Length < 4 || OtpCode.Trim().Length > 10)
        {
            errors.Add(new ErrorDetail("otpCode", "Mã OTP phải có độ dài hợp lệ từ 4 đến 10 ký tự."));
        }

        if (string.IsNullOrWhiteSpace(SignatureMethod))
        {
            errors.Add(new ErrorDetail("signatureMethod", "Phương thức ký không được để trống."));
        }
        else if (SignatureMethod.Length > 50)
        {
            errors.Add(new ErrorDetail("signatureMethod", "Tên phương thức ký không được vượt quá 50 ký tự."));
        }

        return errors;
    }
}
